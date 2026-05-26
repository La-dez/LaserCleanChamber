using LaserCleanChamber.Model.Slicing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using static LaserCleanChamber.Model.Communication.Protocol;

namespace LaserCleanChamber.Model
{
    public class MocChamberDevice : IChamberDevice
    {
        public MocChamberDevice()
        {
        }

        private ChamberDeviceState state = ChamberDeviceState.Idle;
        public ChamberDeviceState State
        {
            get => state;
            set
            {
                state = value;
                OnStateChanged?.Invoke(state);
            }
        }

        public Telemetry Telemetry => new Telemetry(
            false, true, true, true, false, 45);

        public event Action<ChamberDeviceState>? OnStateChanged;
        public event Action<Telemetry>? OnTelemetryUpdated;
        public event Action<string>? OnErrorMessage;
        public event Action<bool>? OnCalibrationDone;

        private bool isCalibrated = false;
        public bool IsCalibrated => isCalibrated;

        private CancellationTokenSource? cancellationTokenSource = null;

        private Task? currentTask = null;

        public void StartCalibrating()
        {
            if (this.State != ChamberDeviceState.Idle)
                return;
            this.State = ChamberDeviceState.Calibrating;

            cancellationTokenSource = new CancellationTokenSource();

            currentTask = Task.Run(() => Calibrating(cancellationTokenSource.Token), cancellationTokenSource.Token);
        }

        private void Calibrating(CancellationToken token)
        {
            try
            {
                token.WaitHandle.WaitOne(4000);
                isCalibrated = true;
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
            finally
            {
                this.OnCalibrationDone?.Invoke(IsCalibrated);
                this.State = ChamberDeviceState.Idle;
            }
        }

        public void StartCleaning(LaserPreset preset, List<PathSegment<g3.Vector3d>> trace)
        {
            if (this.State != ChamberDeviceState.Idle)
                return;
            this.State = ChamberDeviceState.Cleaning;

            cancellationTokenSource = new CancellationTokenSource();

            currentTask = Task.Run(() => Cleaning(preset, trace, cancellationTokenSource.Token), cancellationTokenSource.Token);
        }
        public bool IsTelemetrySaysCleaning()
        {
            // В реальной реализации здесь бы анализировались данные телеметрии, 
            // например, по текущей мощности лазера, состоянию датчиков и т.д.
            // Для мок-устройства просто возвращаем true, если состояние "Cleaning".
            return this.State == ChamberDeviceState.Cleaning;
        }
        private void Cleaning(LaserPreset preset, List<PathSegment<g3.Vector3d>> trace, CancellationToken token)
        {
            try
            {
                token.WaitHandle.WaitOne((int)(100 * preset.ScanSpeed));
            }
            catch (OperationCanceledException) { }
            catch (Exception) { }
            finally
            {
                this.State = ChamberDeviceState.Idle;
            }
        }

        public void StopCleaning()
        {
            if (State != ChamberDeviceState.Cleaning)
                return;

            if (cancellationTokenSource != null && currentTask != null && !currentTask.IsCompleted)
            {
                cancellationTokenSource.Cancel();

                try
                {
                    // Ждем завершения синхронно (блокируем поток, пока не доделается)
                    // Важно: если задача зависла намертво и не проверяет токен, тут программа повиснет.
                    // Можно добавить таймаут: currentTask.Wait(3000); 
                    currentTask.Wait();
                }
                catch (AggregateException)
                {
                    // Task.Wait() заворачивает ошибки в AggregateException, их обычно можно игнорировать при закрытии
                }

                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        public void Dispose()
        {
            if (cancellationTokenSource != null && currentTask != null && !currentTask.IsCompleted)
            {
                cancellationTokenSource.Cancel();

                try
                {
                    currentTask.Wait();
                }
                catch (AggregateException)
                {
                    // Task.Wait() заворачивает ошибки в AggregateException, их обычно можно игнорировать при закрытии
                }

                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        public void SetLaserParameters(LaserPreset preset)
        {

        }

        public void SetCleaningParameters(LaserPreset preset)
        {

        }
    }
}
