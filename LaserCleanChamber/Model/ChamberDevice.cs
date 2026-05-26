using ControlzEx.Standard;
using LaserCleanChamber.Configuration;
using LaserCleanChamber.Model.Communication;
using LaserCleanChamber.Model.LaserComm;
using LaserCleanChamber.Model.Slicing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static LaserCleanChamber.Model.Communication.Protocol;

namespace LaserCleanChamber.Model
{
    public class ChamberDevice : IChamberDevice
    {
        private LaserPortManager laserPortManager;
        private SerialPort serialPort;

        private byte laserSlaveId = 1;

        private ChamberDeviceState state = ChamberDeviceState.Idle;
        public ChamberDeviceState State
        {
            get => state;
            set
            {
                state = value;
                try
                {
                    OnStateChanged?.Invoke(value);
                }
                catch { }
            }
        }

        private Telemetry telemetry;
        public Telemetry Telemetry
        {
            get => telemetry;
            set
            {
                telemetry = value;
                try
                {
                    OnTelemetryUpdated?.Invoke(value);
                }
                catch { }
            }
        }

        private MachineCalibration machineCalibration = new MachineCalibration();

        public event Action<ChamberDeviceState>? OnStateChanged;
        public event Action<Telemetry>? OnTelemetryUpdated;
        public event Action<string>? OnErrorMessage;
        public event Action<bool>? OnCalibrationDone;

        private readonly object serialLocker = new object();
        private readonly object taskLocker = new object();

        public bool IsCalibrated => machineCalibration.IsAllCalibrated;

        private Task? task;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private int timeout_ms = 3000;

        private Task? telemetryTask;
        private CancellationTokenSource ctsTelemetry = new CancellationTokenSource();
        private int telemetryUpdatePeriod = 1500;

        public ChamberDevice(SerialPortConnectionProperties scannerPortPrameters,
            string laserPortName,
            CalibrationSettings calibrationSettings)
        {
            serialPort = new SerialPort(
                scannerPortPrameters.PortName,
                scannerPortPrameters.Baudrate,
                scannerPortPrameters.Parity,
                scannerPortPrameters.DataBits,
                scannerPortPrameters.StopBits);
            serialPort.Open();

            laserPortManager = new LaserPortManager(laserPortName, laserSlaveId);
            laserPortManager.Open();

            //telemetry = new Telemetry(false, false, false, true, false, 25);
            telemetry = ReadTelemetery();

            machineCalibration.CenterOffsetX = calibrationSettings.CenterOffsetX;
            machineCalibration.CenterOffsetY = calibrationSettings.CenterOffsetY;
            machineCalibration.CenterOffsetZ = calibrationSettings.CenterOffsetZ;
            machineCalibration.StepsPerMmZ = calibrationSettings.StepsPerMmZ;
            machineCalibration.StepsPerMmX = calibrationSettings.StepsPerMmX;
            machineCalibration.StepsPerMmY = calibrationSettings.StepsPerMmY;

            StartCalibrating();

            ctsTelemetry = new CancellationTokenSource();
            telemetryTask = Task.Run(() => TelemetryUpdater(ctsTelemetry.Token));
        }

        private void TelemetryUpdater(CancellationToken token)
        {
            try
            {
                while(!ctsTelemetry.IsCancellationRequested)
                {
                    if(task == null || task.Status != TaskStatus.Running)
                    {
                        Telemetry = ReadTelemetery();
                    }
                    token.WaitHandle.WaitOne(telemetryUpdatePeriod);
                }
            }
            catch { }
        }

        public void SetLaserParameters(LaserPreset preset)
        {
            //return;
            byte[] request = ModbusRtuHelper.BuildWriteSingleRequest(laserSlaveId, LaserRegisters.WeldingModeSelect,
                (ushort)WeldingMode.Continuous);
            byte[] response = laserPortManager.SendRequestAndWaitResponse(request);

            request = request = ModbusRtuHelper.BuildWriteSingleRequest(laserSlaveId, LaserRegisters.LaserPowerOutput,
                (ushort)preset.Power);
            response = laserPortManager.SendRequestAndWaitResponse(request);

            var swingSpeedParameter = LaserLimits.Get<ushort>(LaserRegisters.SwingSpeed)
                ?? throw new InvalidOperationException("Не найдены лимиты для скорости колебаний.");
            ushort swingSpeedValue = Convert.ToUInt16(Math.Round(preset.ScanSpeed));
            ushort swingSpeedModbus = swingSpeedParameter.ToModbusValue(swingSpeedValue);

            request = ModbusRtuHelper.BuildWriteSingleRequest(laserSlaveId, LaserRegisters.SwingSpeed,
                swingSpeedModbus);
            response = laserPortManager.SendRequestAndWaitResponse(request);

            request = ModbusRtuHelper.BuildWriteSingleRequest(laserSlaveId, LaserRegisters.SwingWidth,
                (ushort)(preset.ScanWidth * 10));
            response = laserPortManager.SendRequestAndWaitResponse(request);
        }

        public void SetCleaningParameters(LaserPreset preset)
        {
            if (State == ChamberDeviceState.Calibrating)
                return;

            Frame cooldownAndRepeatsRequest = EncodeSetCooldownAndRepeatsCleaning(
                preset.CleaningRepeats,
                preset.CooldownBetweenPassesSeconds);
            Send(cooldownAndRepeatsRequest);

            Frame cooldownLinesRequest = EncodeSetCooldownLines(preset.CooldownAfterLinesSeconds);
            Send(cooldownLinesRequest);
        }

        private void StopTask()
        {
            lock(taskLocker)
            {
                if(task != null && !task.IsCompleted)
                {
                    cts.Cancel();
                    task?.Wait();
                    task = null;
                }
            }

            //wait to stop...
        }

        public void StartCalibrating()
        {
            lock(taskLocker)
            {
                if(state != ChamberDeviceState.Idle)
                    return;

                cts = new CancellationTokenSource();
                State = ChamberDeviceState.Calibrating;
                task = Task.Run(() => CalibratingProcess(cts.Token), cts.Token);
            }
        }

        private Telemetry ReadTelemetery()
        {
            var request = Protocol.EncodeGetTelemetery();
            var responce = SendAndWaitReply(request, cts.Token, timeout_ms);

            Telemetry telemetry = Protocol.DecodeTelemetry(responce);
            return telemetry;
        }
        public bool IsTelemetrySaysCleaning()
        {
            var telemetry = ReadTelemetery();
            return telemetry.IsCleaning;
        }
        uint calibrateAxisSync(MotorAxis axis, CancellationToken token, int timeout_ms)
        {
            var request = EncodeStmpCalibration(axis);
            var responce = SendAndWaitReply(request, token, timeout_ms);
            (MotorAxis respAxis, uint steps) = DecodeStmpCalibration(responce);
            if (axis != respAxis)
                throw new Exception("Responce Axis mismatch");
            return steps;
        }

        private static void checkCalibrationResult(MotorAxis axis, uint steps, MachineCalibration calibration)
        {
            switch(axis)
            {
                case MotorAxis.X:
                    calibration.MaxStepsX = steps;
                    calibration.IsXCalibrated = true;
                    break;
                case MotorAxis.Y:
                    calibration.MaxStepsY = steps;
                    calibration.IsYCalibrated = true;
                    break;
                case MotorAxis.Z:
                    calibration.MaxStepsZ = steps;
                    calibration.IsZCalibrated = true;
                    break;
            }
        }

        private void CalibratingProcess(CancellationToken token)
        {
            Exception? exception;
            try
            {
                Frame requestCalibX = EncodeStmpCalibration(MotorAxis.X);
                Frame requestCalibY = EncodeStmpCalibration(MotorAxis.Y);
                Frame requestCalibZ = EncodeStmpCalibration(MotorAxis.Z);

                Send(requestCalibX);
                Send(requestCalibY);
                Send(requestCalibZ);

                Frame responce1 = WaitReply(MessageType.STMP_CALIBRATION, token);
                Frame responce2 = WaitReply(MessageType.STMP_CALIBRATION, token);
                Frame responce3 = WaitReply(MessageType.STMP_CALIBRATION, token);

                (MotorAxis axis1, uint steps1) = DecodeStmpCalibration(responce1);
                (MotorAxis axis2, uint steps2) = DecodeStmpCalibration(responce2);
                (MotorAxis axis3, uint steps3) = DecodeStmpCalibration(responce3);

                checkCalibrationResult(axis1, steps1, machineCalibration);
                checkCalibrationResult(axis2, steps2, machineCalibration);
                checkCalibrationResult(axis3, steps3, machineCalibration);

                Thread.Sleep(300);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                this.OnCalibrationDone?.Invoke(IsCalibrated);
                State = ChamberDeviceState.Idle;
            }
        }

        private TracePoint CreateTracePoint(g3.Vector3d p, bool laserOn)
        {
            double machineX = machineCalibration.ToMachineX(p.x);
            double machineY = machineCalibration.ToMachineY(p.y);
            double machineZ = machineCalibration.ToMachineZ(p.z);

            if (machineX < 0 || machineX > machineCalibration.MaxStepsX ||
                machineY < 0 || machineY > machineCalibration.MaxStepsY ||
                machineZ < 0 || machineZ > machineCalibration.MaxStepsZ)
            {
                throw new ArgumentOutOfRangeException(
                    $"Trace point is out of machine physical bounds! " +
                    $"Calculated Steps: X={machineX:F0}, Y={machineY:F0}, Z={machineZ:F0}. " +
                    "Please check the 3D model position or ROI limits.");
            }

            TracePoint localPoint = new TracePoint
            {
                X = (uint)Math.Round(machineX),
                Y = (uint)Math.Round(machineY),
                Z = (uint)Math.Round(machineZ),
                LaserOn = laserOn
            };

            return localPoint;
        }

        private List<TracePoint> PrepareTraceInMotorCoordinates(List<PathSegment<g3.Vector3d>> trace)
        {
            if (trace.Count < 1)
                throw new Exception("No trace points");

            if (!machineCalibration.IsAllCalibrated)
                throw new InvalidOperationException("Cannot transform coordinates. Machine is not calibrated.");

            List<TracePoint> localTrace = new List<TracePoint>(trace.Count + 1);

            for (int i = 0; i < trace.Count; i++)
            {
                var tp0 = CreateTracePoint(trace[i].p0, false);
                var tp1 = CreateTracePoint(trace[i].p1, trace[i].laserOn);

                bool addPointAsNew = localTrace.Count == 0 || (trace[i - 1].p1 - trace[i].p0).Length > 1;
                if (addPointAsNew)
                {
                    localTrace.Add(tp0);
                }
                else
                {
                    tp0.LaserOn = trace[i - 1].laserOn;
                    localTrace[localTrace.Count - 1] = tp0;
                }
                localTrace.Add(tp1);
            }
            var tpEnd = CreateTracePoint(trace.Last().p1, false);
            localTrace.Add(tpEnd);

            return localTrace;
        }

        public void StartCleaning(LaserPreset preset, List<PathSegment<g3.Vector3d>> trace)
        {
            lock (taskLocker)
            {
                if (state != ChamberDeviceState.Idle)
                    return;

                if (!machineCalibration.IsAllCalibrated)
                    throw new Exception("Cannot start cleaning. No calibration");

                Telemetry = ReadTelemetery();
                //TODO расписать ошибки подробнее
                /*if (telemetry.IsError || telemetry.IsCleaning || !telemetry.DoorClosed || !telemetry.PlatePlaced || !telemetry.PistolPlaced)
                    throw new Exception("Невозможно начать отчистку");*/

                cts = new CancellationTokenSource();
                State = ChamberDeviceState.Cleaning;

                task = Task.Run(() => CleaningProcess(preset, trace, cts.Token), cts.Token);
            }
        }

        private void CleaningProcess(LaserPreset preset, List<PathSegment<g3.Vector3d>> trace, CancellationToken token)
        {
            Exception? exception;
            bool cleaningStarted = false;
            try
            {
                SetLaserParameters(preset);
                SetCleaningParameters(preset);

                List<TracePoint> localTrace = PrepareTraceInMotorCoordinates(trace);

                SendTrajectory(localTrace, token);

                cleaningStarted = true;

                Frame startRequest = EncodeLaserCleanSwitch(true);
                SendAndWaitReply(startRequest, token, 1500);

                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(1000);
                    try
                    {
                        Telemetry = ReadTelemetery();
                        bool isCleaning = telemetry.IsCleaning;
                        if (!isCleaning)
                        {
                            break;
                        }
                    }
                    catch(OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    { }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                exception = ex;
                OnErrorMessage?.Invoke(ex.ToString());
            }
            finally
            {
                State = ChamberDeviceState.Idle;
            }
        }

        public void StopCleaning()
        {
            try
            {
                Frame stopRequest = EncodeLaserCleanSwitch(false);
                SendAndWaitReply(stopRequest, new CancellationToken(), 1000);
            }
            catch { }
        }



        private const int MaxPointsCount = 1500;
        private const int ChunkPayloadMaxBytes = 70;

        private void SendTrajectory(List<TracePoint> trace, CancellationToken token)
        {
            int pointsInChank = ChunkPayloadMaxBytes / default(TracePoint).SizeInBytes;

            if (trace.Count > MaxPointsCount)
                throw new Exception("Слишком длинная траектория");

            TracePoint[] tracePoints = trace.ToArray();
            using(StreamWriter sw = new StreamWriter(DateTime.Now.ToString("dd-MM-yyyy_mm-ss") + ".txt"))
            {
                for(int i = 0; i <  tracePoints.Length; i++)
                {
                    sw.WriteLine(tracePoints[i].ToString());
                }
            }

            int index = 0;
            while (true)
            {
                int remains = tracePoints.Length - index;
                int toSend = Math.Min(remains, pointsInChank);
                List<TracePoint> tracePart = new List<TracePoint>();
                tracePart.AddRange(new ReadOnlySpan<TracePoint>(tracePoints, index, toSend));

                int tryNumberMax = 3;
                for (int tryNum = 0; tryNum < tryNumberMax;)
                {
                    Frame request = EncodeSendTraectoryPart((ushort)index, tracePart);

                    List<byte> binaryTrajectory = new List<byte>();
                    binaryTrajectory.AddRange(request.Payload);

                    Frame responce = SendAndWaitReply(request, token);
                    var result = DecodeSendTrajectoryResult(responce);
                    if (result.success && result.readedBytes == request.PayloadLength)
                        break;
                    tryNum++;

                    if (tryNum >= tryNumberMax)
                        throw new Exception("Ошибка отправки траектории");
                }

                index += toSend;

                if (index >= tracePoints.Length)
                    break;
            }
        }

        private Frame WaitReply(MessageType messageType, CancellationToken token, int timeout_ms = -1)
        {
            lock (serialLocker)
            {
                FrameBuilder frameBuilder = new FrameBuilder();
                Stopwatch sw = Stopwatch.StartNew();
                while (true)
                {
                    if (serialPort.BytesToRead > 0)
                    {
                        var b = serialPort.ReadByte();
                        if (b >= 0)
                        {
                            if (frameBuilder.ProcessByte((byte)b, out Frame? responce))
                            {
                                if (responce != null)
                                {
                                    /*if (responce.MessageType != request.MessageType)
                                        throw new Exception("Request and responce message type mismatch");
                                    */
                                    return responce;
                                }
                                else
                                    throw new NullReferenceException("No responce");
                            }
                        }
                    }
                    else
                        token.WaitHandle.WaitOne(5);

                    if (token.IsCancellationRequested)
                        throw new OperationCanceledException();
                    if (timeout_ms >= 0 && sw.Elapsed.TotalMilliseconds > timeout_ms)
                        throw new TimeoutException();
                }
            }
            throw new Exception("Unknown error");
        }

        private Frame SendAndWaitReply(Frame request, CancellationToken token, int timeout_ms = -1, int timeoutMaxTrys = 3)
        {
            lock (serialLocker)
            {
                int tryNum = 1;
                for (; tryNum <= timeoutMaxTrys; tryNum++)
                {
                    try
                    {
                        Send(request);
                        return WaitReply((MessageType)request.MessageType, token, timeout_ms);
                    }
                    catch (TimeoutException) { }
                }
                throw new Exception("Timeout waiting reply");
            }
        }

        private void Send(Frame request)
        {
            lock (serialLocker)
            {
                if (!serialPort.IsOpen)
                    throw new Exception("Port closed");

                var requestBuffer = request.ToByteArray();
                serialPort.DiscardInBuffer();
                serialPort.Write(requestBuffer, 0, requestBuffer.Length);
            }
        }

        private bool isDisposed = false;
        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            
            try
            {
                StopTask();

                serialPort.Close();
                serialPort.Dispose();
            }
            catch { }
            try
            {
                //laserPortManager.Close();
                //laserPortManager.Dispose();
            }
            catch {}
            GC.SuppressFinalize(this);
        }
    }
}
