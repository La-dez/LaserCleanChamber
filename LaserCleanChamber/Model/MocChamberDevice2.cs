using LaserCleanChamber.Configuration;
using LaserCleanChamber.Logging;
using LaserCleanChamber.Logging.TrajectoryDiagnostics;
using LaserCleanChamber.Model.Communication;
using LaserCleanChamber.Model.Path;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static LaserCleanChamber.Model.Communication.Protocol;

namespace LaserCleanChamber.Model
{
    public class MocChamberDevice2 : IChamberDevice
    {
        private readonly object taskLocker = new object();

        private ChamberDeviceState state = ChamberDeviceState.Idle;
        public ChamberDeviceState State
        {
            get => state;
            private set
            {
                state = value;
                try
                {
                    OnStateChanged?.Invoke(value);
                }
                catch { }
            }
        }

        private Telemetry telemetry = new Telemetry(false, true, true, true, false, 25);
        public Telemetry Telemetry
        {
            get => telemetry;
            private set
            {
                telemetry = value;
                try
                {
                    OnTelemetryUpdated?.Invoke(value);
                }
                catch { }
            }
        }

        private readonly MachineCalibration machineCalibration = new MachineCalibration();
        public bool IsCalibrated => machineCalibration.IsAllCalibrated;

        public event Action<ChamberDeviceState>? OnStateChanged;
        public event Action<Telemetry>? OnTelemetryUpdated;
        public event Action<string>? OnErrorMessage;
        public event Action<bool>? OnCalibrationDone;

        private Task? task;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private int timeout_ms = 3000;

        private bool isCleaningEmulated;
        private DateTime cleaningEndsAtUtc = DateTime.MinValue;
        private bool isDisposed;

        private const int MaxPointsCount = 1500;
        private const int ChunkPayloadMaxBytes = 70;
        private readonly bool isTrajectoryDiagnosticsJsonEnabled;

        private static string CreateTrajectoryDiagnosticsPath()
        {
            string diagnosticsDirectory = System.IO.Path.Combine(AppContext.BaseDirectory, "TrajectoryDiagnostics");
            Directory.CreateDirectory(diagnosticsDirectory);
            return System.IO.Path.Combine(diagnosticsDirectory, $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-emulated-trajectory.json");
        }

        public MocChamberDevice2(CalibrationSettings calibrationSettings)
        {
            machineCalibration.CenterOffsetX = calibrationSettings.CenterOffsetX;
            machineCalibration.CenterOffsetY = calibrationSettings.CenterOffsetY;
            machineCalibration.CenterOffsetZ = calibrationSettings.CenterOffsetZ;
            machineCalibration.StepsPerMmZ = calibrationSettings.StepsPerMmZ;
            machineCalibration.StepsPerMmX = calibrationSettings.StepsPerMmX;
            machineCalibration.StepsPerMmY = calibrationSettings.StepsPerMmY;
            isTrajectoryDiagnosticsJsonEnabled = SettingsManager.Load().Logging.EnabledTrajectoryDiagnosticsJson;

            StartCalibrating();
            Telemetry = ReadTelemetery();
        }

        private static TrajectoryPointSummary BuildPointSummary(IReadOnlyList<TracePoint> points)
        {
            return new TrajectoryPointSummary
            {
                Count = points.Count,
                MinX = points.Min(p => p.X),
                MaxX = points.Max(p => p.X),
                MinY = points.Min(p => p.Y),
                MaxY = points.Max(p => p.Y),
                MinZ = points.Min(p => p.Z),
                MaxZ = points.Max(p => p.Z),
                LaserOnPoints = points.Count(p => p.LaserOn),
                FirstPoints = points.Take(5).Select(ToSnapshot).ToList(),
                LastPoints = points.Skip(Math.Max(0, points.Count - 5)).Select(ToSnapshot).ToList()
            };
        }

        private static TracePointSnapshot ToSnapshot(TracePoint point)
        {
            return new TracePointSnapshot
            {
                X = point.X,
                Y = point.Y,
                Z = point.Z,
                LaserOn = point.LaserOn
            };
        }

        public void SetLaserParameters(LaserPreset preset)
        {
            AppLogging.App.Information(AppLogging.Prefix("APP", "Action=ApplyLaserParametersEmulated, Preset={PresetName}, Power={Power}, ScanSpeed={ScanSpeed}, ScanWidth={ScanWidth}"), preset.Name, preset.Power, preset.ScanSpeed, preset.ScanWidth);
        }

        public void SetCleaningParameters(LaserPreset preset)
        {
            if (State == ChamberDeviceState.Calibrating)
                return;

            AppLogging.App.Information(AppLogging.Prefix("APP", "Action=ApplyCleaningParametersEmulated, Preset={PresetName}, CleaningRepeats={CleaningRepeats}, CooldownBetweenPassesSeconds={CooldownBetweenPassesSeconds}, CooldownAfterLinesSeconds={CooldownAfterLinesSeconds}"),
                preset.Name,
                preset.CleaningRepeats,
                preset.CooldownBetweenPassesSeconds,
                preset.CooldownAfterLinesSeconds);

            Frame cooldownAndRepeatsRequest = EncodeSetCooldownAndRepeatsCleaning(
                preset.CleaningRepeats,
                preset.CooldownBetweenPassesSeconds);
            Send(cooldownAndRepeatsRequest);

            Frame cooldownLinesRequest = EncodeSetCooldownLines(preset.CooldownAfterLinesSeconds);
            Send(cooldownLinesRequest);
        }

        public void StartCalibrating()
        {
            lock (taskLocker)
            {
                if (State != ChamberDeviceState.Idle)
                    return;

                cts = new CancellationTokenSource();
                State = ChamberDeviceState.Calibrating;
                AppLogging.App.Information(AppLogging.Prefix("APP", "Action=StartCalibrationEmulated"));
                task = Task.Run(() => CalibratingProcess(cts.Token), cts.Token);
            }
        }

        public void StartCleaning(LaserPreset preset, List<PathSegment<g3.Vector3d>> trace)
        {
            lock (taskLocker)
            {
                if (State != ChamberDeviceState.Idle)
                    return;

                if (!machineCalibration.IsAllCalibrated)
                    throw new Exception("Cannot start cleaning. No calibration");

                Telemetry = ReadTelemetery();

                cts = new CancellationTokenSource();
                State = ChamberDeviceState.Cleaning;
                AppLogging.App.Information(AppLogging.Prefix("APP", "Action=StartCleaningEmulated, Preset={PresetName}, Segments={Segments}"), preset.Name, trace.Count);

                task = Task.Run(() => CleaningProcess(preset, trace, cts.Token), cts.Token);
            }
        }

        public void StopCleaning()
        {
            AppLogging.App.Information(AppLogging.Prefix("APP", "Action=StopCleaningRequestedEmulated"));
            isCleaningEmulated = false;

            try
            {
                Frame stopRequest = EncodeLaserCleanSwitch(false);
                SendAndWaitReply(stopRequest, CancellationToken.None, 1000);
                AppLogging.App.Information(AppLogging.Prefix("APP", "Action=StopCleaningConfirmedEmulated"));
            }
            catch
            {
                AppLogging.App.Error(AppLogging.Prefix("APP", "Action=StopCleaningFailedEmulated"));
                throw;
            }
        }

        public bool IsTelemetrySaysCleaning()
        {
            var currentTelemetry = ReadTelemetery();
            return currentTelemetry.IsCleaning;
        }

        private void CalibratingProcess(CancellationToken token)
        {
            Exception? exception = null;
            try
            {
                Frame requestCalibX = EncodeStmpCalibration(MotorAxis.X);
                Frame requestCalibY = EncodeStmpCalibration(MotorAxis.Y);
                Frame requestCalibZ = EncodeStmpCalibration(MotorAxis.Z);

                Frame response1 = SendAndWaitReply(requestCalibX, token, timeout_ms);
                Frame response2 = SendAndWaitReply(requestCalibY, token, timeout_ms);
                Frame response3 = SendAndWaitReply(requestCalibZ, token, timeout_ms);

                (MotorAxis axis1, uint steps1) = DecodeStmpCalibration(response1);
                (MotorAxis axis2, uint steps2) = DecodeStmpCalibration(response2);
                (MotorAxis axis3, uint steps3) = DecodeStmpCalibration(response3);

                CheckCalibrationResult(axis1, steps1);
                CheckCalibrationResult(axis2, steps2);
                CheckCalibrationResult(axis3, steps3);

                Thread.Sleep(300);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                exception = ex;
                AppLogging.App.Error(ex, AppLogging.Prefix("APP", "Action=CalibrationFailedEmulated"));
            }
            finally
            {
                if (exception == null)
                {
                    AppLogging.App.Information(AppLogging.Prefix("APP", "Action=CalibrationCompletedEmulated, Success={Success}"), IsCalibrated);
                }

                OnCalibrationDone?.Invoke(IsCalibrated);
                State = ChamberDeviceState.Idle;
                Telemetry = ReadTelemetery();
            }
        }

        private void CleaningProcess(LaserPreset preset, List<PathSegment<g3.Vector3d>> trace, CancellationToken token)
        {
            Exception? exception = null;
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

                int simulatedDurationMs = Math.Max(1000, Math.Min(30000, localTrace.Count * 10));
                cleaningEndsAtUtc = DateTime.UtcNow.AddMilliseconds(simulatedDurationMs);

                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(250);

                    if (isCleaningEmulated && DateTime.UtcNow >= cleaningEndsAtUtc)
                    {
                        isCleaningEmulated = false;
                    }

                    Telemetry = ReadTelemetery();
                    if (!Telemetry.IsCleaning)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                exception = ex;
                AppLogging.App.Error(ex, AppLogging.Prefix("APP", "Action=CleaningFailedEmulated"));
                OnErrorMessage?.Invoke(ex.ToString());
            }
            finally
            {
                isCleaningEmulated = false;
                Telemetry = ReadTelemetery();
                AppLogging.App.Information(AppLogging.Prefix("APP", "Action=CleaningCompletedEmulated, Started={Started}, CancelRequested={CancelRequested}, Error={HasError}"), cleaningStarted, token.IsCancellationRequested, exception != null);
                State = ChamberDeviceState.Idle;
            }
        }

        private Telemetry ReadTelemetery()
        {
            var request = Protocol.EncodeGetTelemetery();
            var response = SendAndWaitReply(request, CancellationToken.None, timeout_ms);

            Telemetry telemetry = Protocol.DecodeTelemetry(response);
            AppLogging.Telemetry.Information(AppLogging.Prefix("TEL", "Action=TelemetryReadEmulated, DoorClosed={DoorClosed}, PlatePlaced={PlatePlaced}, PistolPlaced={PistolPlaced}, IsCleaning={IsCleaning}, IsError={IsError}, TemperatureInside_degC={TemperatureInside_degC}"),
                telemetry.DoorClosed,
                telemetry.PlatePlaced,
                telemetry.PistolPlaced,
                telemetry.IsCleaning,
                telemetry.IsError,
                telemetry.TemperatureInside_degC);
            return telemetry;
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

            return new TracePoint
            {
                X = (uint)Math.Round(machineX),
                Y = (uint)Math.Round(machineY),
                Z = (uint)Math.Round(machineZ),
                LaserOn = laserOn
            };
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

        private static string DescribeTracePoints(IReadOnlyList<TracePoint> trace)
        {
            if (trace == null || trace.Count == 0)
                return "Count=0";

            uint minX = trace.Min(p => p.X);
            uint maxX = trace.Max(p => p.X);
            uint minY = trace.Min(p => p.Y);
            uint maxY = trace.Max(p => p.Y);
            uint minZ = trace.Min(p => p.Z);
            uint maxZ = trace.Max(p => p.Z);

            string firstPoints = string.Join(" | ", trace.Take(5).Select(p => p.ToString()));
            string lastPoints = string.Join(" | ", trace.Skip(Math.Max(0, trace.Count - 5)).Select(p => p.ToString()));

            return $"Count={trace.Count}, RangeX={minX}-{maxX}, RangeY={minY}-{maxY}, RangeZ={minZ}-{maxZ}, First=[{firstPoints}], Last=[{lastPoints}]";
        }

        private void SendTrajectory(List<TracePoint> trace, CancellationToken token)
        {
            int pointsInChank = ChunkPayloadMaxBytes / default(TracePoint).SizeInBytes;

            if (trace.Count > MaxPointsCount)
                throw new Exception("Слишком длинная траектория");

            TracePoint[] tracePoints = trace.ToArray();
            int totalChunks = (tracePoints.Length + pointsInChank - 1) / pointsInChank;
            string? diagnosticsPath = isTrajectoryDiagnosticsJsonEnabled ? CreateTrajectoryDiagnosticsPath() : null;
            TrajectoryDiagnosticsRecord? diagnosticsRecord = isTrajectoryDiagnosticsJsonEnabled
                ? new TrajectoryDiagnosticsRecord
                {
                    Timestamp = DateTimeOffset.Now,
                    Source = "MocChamberDevice2",
                    IsEmulated = true,
                    MaxPoints = MaxPointsCount,
                    PointsPerChunk = pointsInChank,
                    ChunkPayloadMaxBytes = ChunkPayloadMaxBytes,
                    PointSizeBytes = default(TracePoint).SizeInBytes,
                    TotalPoints = tracePoints.Length,
                    TotalChunks = totalChunks,
                    PayloadBytesTotal = 0,
                    SentBytesTotal = 0,
                    Summary = BuildPointSummary(tracePoints),
                    Chunks = new List<TrajectoryChunkRecord>()
                }
                : null;

            AppLogging.App.Information(
                AppLogging.Prefix("APP", "Action=TrajectoryPreparedForControllerEmulated, Points={Points}, MaxPoints={MaxPoints}, PointsPerChunk={PointsPerChunk}, Chunks={Chunks}, ChunkPayloadMaxBytes={ChunkPayloadMaxBytes}, TraceSummary={TraceSummary}"),
                tracePoints.Length,
                MaxPointsCount,
                pointsInChank,
                totalChunks,
                ChunkPayloadMaxBytes,
                DescribeTracePoints(tracePoints));

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

                    AppLogging.Controller.Information(
                        AppLogging.Prefix("CTRL", "Action=TrajectoryChunkSendEmulated, ChunkIndex={ChunkIndex}, TotalChunks={TotalChunks}, StartIndex={StartIndex}, Points={Points}, PayloadLength={PayloadLength}, FrameLength={FrameLength}, ChunkSummary={ChunkSummary}"),
                        (index / pointsInChank) + 1,
                        totalChunks,
                        index,
                        tracePart.Count,
                        request.PayloadLength,
                        request.FrameLength,
                        DescribeTracePoints(tracePart));

                    Frame response = SendAndWaitReply(request, token);
                    var result = DecodeSendTrajectoryResult(response);

                    diagnosticsRecord?.Chunks.Add(new TrajectoryChunkRecord
                    {
                        ChunkIndex = (index / pointsInChank) + 1,
                        StartIndex = index,
                        Points = tracePart.Count,
                        PayloadLength = request.PayloadLength,
                        FrameLength = request.FrameLength,
                        AckSuccess = result.success,
                        AckReadedBytes = result.readedBytes,
                        Summary = BuildPointSummary(tracePart)
                    });

                    if (diagnosticsRecord != null)
                    {
                        diagnosticsRecord.PayloadBytesTotal += request.PayloadLength;
                        diagnosticsRecord.SentBytesTotal += request.FrameLength;
                    }

                    AppLogging.Controller.Information(
                        AppLogging.Prefix("CTRL", "Action=TrajectoryChunkAckEmulated, ChunkIndex={ChunkIndex}, TotalChunks={TotalChunks}, StartIndex={StartIndex}, Success={Success}, ReadedBytes={ReadedBytes}, ExpectedBytes={ExpectedBytes}"),
                        (index / pointsInChank) + 1,
                        totalChunks,
                        index,
                        result.success,
                        result.readedBytes,
                        request.PayloadLength);

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

            if (diagnosticsRecord != null && diagnosticsPath != null)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(diagnosticsPath, JsonSerializer.Serialize(diagnosticsRecord, options));
                AppLogging.App.Information(AppLogging.Prefix("APP", "Action=TrajectoryDiagnosticsSavedEmulated, Path={Path}"), diagnosticsPath);
            }
        }

        private Frame SendAndWaitReply(Frame request, CancellationToken token, int timeout_ms = -1)
        {
            token.ThrowIfCancellationRequested();
            Send(request);
            var response = EmulateReply(request);
            AppLogging.Controller.Debug(AppLogging.Prefix("CTRL", "Direction=RX, {Description}; Hex={Hex}"), AppLogging.DescribeFrame(response), AppLogging.ToHex(response.ToByteArray()));
            return response;
        }

        private void Send(Frame request)
        {
            AppLogging.Controller.Debug(AppLogging.Prefix("CTRL", "Direction=TX, {Description}; Hex={Hex}"), AppLogging.DescribeFrame(request), AppLogging.ToHex(request.ToByteArray()));
        }

        private Frame EmulateReply(Frame request)
        {
            switch ((MessageType)request.MessageType)
            {
                case MessageType.MSG_TELEMETRY:
                    return BuildTelemetryResponse();
                case MessageType.STMP_CALIBRATION:
                    return BuildCalibrationResponse(request);
                case MessageType.SEND_TRAECTORY:
                    return BuildTrajectoryAckResponse(request);
                case MessageType.MSG_LASER_CLEAN_SWITCH:
                    return BuildLaserCleanSwitchResponse(request);
                default:
                    return new Frame(request.MessageType, request.Payload);
            }
        }

        private Frame BuildTelemetryResponse()
        {
            byte status = 0;
            bool isError = false;
            bool doorClosed = true;
            bool platePlaced = true;
            bool pistolPlaced = true;

            if (isError)
                status |= (byte)(1 << 7);
            if (!doorClosed)
                status |= (byte)(1 << 3);
            if (!pistolPlaced)
                status |= (byte)(1 << 4);
            if (!platePlaced)
                status |= (byte)(1 << 5);

            byte cleaning = (byte)(isCleaningEmulated ? 1 : 0);
            return new Frame((byte)MessageType.MSG_TELEMETRY, new byte[] { status, cleaning, 0 });
        }

        private Frame BuildCalibrationResponse(Frame request)
        {
            if (request.PayloadLength < 1)
                throw new Exception("Calibration request payload is invalid.");

            MotorAxis axis = (MotorAxis)request.Payload[0];
            uint steps = axis switch
            {
                MotorAxis.X => machineCalibration.MaxStepsX,
                MotorAxis.Y => machineCalibration.MaxStepsY,
                MotorAxis.Z => machineCalibration.MaxStepsZ,
                _ => 0
            };

            List<byte> payload = new List<byte> { (byte)axis };
            payload.Add((byte)((steps >> 24) & 0xFF));
            payload.Add((byte)((steps >> 16) & 0xFF));
            payload.Add((byte)((steps >> 8) & 0xFF));
            payload.Add((byte)(steps & 0xFF));
            return new Frame((byte)MessageType.STMP_CALIBRATION, payload.ToArray());
        }

        private Frame BuildTrajectoryAckResponse(Frame request)
        {
            return new Frame((byte)MessageType.SEND_TRAECTORY, new byte[] { (byte)request.PayloadLength, 0 });
        }

        private Frame BuildLaserCleanSwitchResponse(Frame request)
        {
            bool isOn = request.PayloadLength > 0 && request.Payload[0] == 1;
            isCleaningEmulated = isOn;
            if (!isOn)
            {
                cleaningEndsAtUtc = DateTime.MinValue;
            }

            return new Frame((byte)MessageType.MSG_LASER_CLEAN_SWITCH, request.Payload);
        }

        private void CheckCalibrationResult(MotorAxis axis, uint steps)
        {
            switch (axis)
            {
                case MotorAxis.X:
                    machineCalibration.MaxStepsX = steps;
                    machineCalibration.IsXCalibrated = true;
                    break;
                case MotorAxis.Y:
                    machineCalibration.MaxStepsY = steps;
                    machineCalibration.IsYCalibrated = true;
                    break;
                case MotorAxis.Z:
                    machineCalibration.MaxStepsZ = steps;
                    machineCalibration.IsZCalibrated = true;
                    break;
            }
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            try
            {
                if (task != null && !task.IsCompleted)
                {
                    cts.Cancel();
                    task.Wait();
                }
            }
            catch { }
            finally
            {
                cts.Dispose();
            }
        }

    }
}
