using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LaserCleanChamber.Configuration;
using LaserCleanChamber.Model;
using LaserCleanChamber.View.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFMediaKit.DirectShow.Controls;
using static LaserCleanChamber.ViewModel.MvvmMessages;

namespace LaserCleanChamber.ViewModel
{
    public partial class ChamberViewModel : ObservableObject, IDisposable, IRecipient<NeedRecalculateTraceMessage>
    {
        private readonly AppSettings settings;

        private readonly IChamberDevice chamberDevice;

        private readonly CleaningSession cleaningSession;

        [ObservableProperty]
        private PresetsViewModel presetsViewModel;

        [ObservableProperty]
        private TracingViewModel tracingViewModel;

        public bool IsIdle => chamberDevice.State == ChamberDeviceState.Idle;
        public bool IsCleaning =>chamberDevice.State == ChamberDeviceState.Cleaning;
        public bool IsCalibrating =>chamberDevice.State == ChamberDeviceState.Calibrating;
        public bool IsError => chamberDevice.State == ChamberDeviceState.Error;

        [ObservableProperty]
        public bool isDoorClosed = false;
        [ObservableProperty]
        public bool isPistolPlaced = false;
        [ObservableProperty]
        public bool isPlatePlaced = false;

        //public string USBCameraName { get; private set; } = "";

        //[ObservableProperty]
        private string? videoSourceName = "";
        public string? VideoSourceName
        {
            get => videoSourceName;
            set
            {
                videoSourceName = value;
                OnPropertyChanged(nameof(VideoSourceName));
            }
        }

        public ChamberViewModel(AppSettings settings)
        {
            this.settings = settings;
            this.cleaningSession = new CleaningSession(settings);

            this.tracingViewModel = new TracingViewModel(cleaningSession);
            this.presetsViewModel = new PresetsViewModel(cleaningSession);

            CameraPlay();

            this.chamberDevice = new MocChamberDevice();

            cleaningSession.CalculateTrajectory();

            WeakReferenceMessenger.Default.Register(this);
        }

        [RelayCommand]
        public void CameraPlay()
        {
            VideoSourceName = cleaningSession.AutoSelectCamera(settings.Hardware.VideoDeviceName);
        }

        public ChamberViewModel(IChamberDevice chamberDevice, AppSettings settings) : this(settings)
        {
            this.chamberDevice = chamberDevice;
            this.chamberDevice.OnErrorMessage += ChamberDevice_OnErrorMessage;
            this.chamberDevice.OnCalibrationDone += ChamberDevice_OnCalibrationDone;
            this.chamberDevice.OnStateChanged += ChamberDevice_OnStateChanged;
            this.chamberDevice.OnTelemetryUpdated += ChamberDevice_OnTelemetryUpdated;
            ChamberDevice_OnTelemetryUpdated(chamberDevice.Telemetry);

            ApplyPreset();
        }

        private void ChamberDevice_OnErrorMessage(string errorMessage)
        {
            //MessageBox.Show($"Ошибка: {errorMessage}");
        }

        private void ChamberDevice_OnTelemetryUpdated(Telemetry telemetry)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                this.IsPistolPlaced = telemetry.PistolPlaced;
                this.IsDoorClosed = telemetry.DoorClosed;
                this.IsPlatePlaced = telemetry.PlatePlaced;
            });
        }

        private void ChamberDevice_OnCalibrationDone(bool isSuccess)
        {
            if (!isSuccess)
                return;

            //Application.Current.Dispatcher.BeginInvoke(() =>
            //{
            //    ApplyCleaningParameters();
            //});
        }

        private void ChamberDevice_OnStateChanged(ChamberDeviceState obj)
        {
            OnPropertyChanged(nameof(IsIdle));
            OnPropertyChanged(nameof(IsError));
            OnPropertyChanged(nameof(IsCleaning));
            OnPropertyChanged(nameof(IsCalibrating));
        }

        [RelayCommand]
        public void StartCleaning()
        {
            if(!IsDoorClosed)
            {
                MessageBox.Show("Невозможно начать отчистку: дверь камеры открыта.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                var preset = this.PresetsViewModel.SelectedPreset;
                var trace = this.TracingViewModel.Trace;
                if (preset != null && trace != null)
                    this.chamberDevice.StartCleaning(preset, trace);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось запустить процесс отчистки: {ex.Message}");
            }
        }

        [RelayCommand]
        public void StopCleaning()
        {
            bool wasCleaning = this.chamberDevice.IsTelemetrySaysCleaning();
            try
            {
                this.chamberDevice.StopCleaning();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось остановить процесс отчистки: {ex.Message}");
            }
            if(!wasCleaning)
            {
                MessageBox.Show($"Команда остановки была отправлена, но устройство сообщает, " +
                                $"что очистка не происходила во время запроса остановки", 
                                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void OpenPresetsEditor()
        {
            try
            {
                ModesWindow modesWindow = new ModesWindow();
                modesWindow.Owner = Application.Current.MainWindow;
                modesWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось открыть редаткор предустановок: {ex.Message}");
            }
        }

        bool isDisposed = false;
        public void Dispose()
        {
            if (chamberDevice == null || isDisposed)
                return;

            isDisposed = true;
            try
            {
                this.chamberDevice.OnErrorMessage -= ChamberDevice_OnErrorMessage;
                this.chamberDevice.OnCalibrationDone -= ChamberDevice_OnCalibrationDone;
                this.chamberDevice.OnStateChanged -= ChamberDevice_OnStateChanged;
                this.chamberDevice.OnTelemetryUpdated -= ChamberDevice_OnTelemetryUpdated;

                this.chamberDevice.Dispose();
            }
            catch { }
        }

        public void SaveLastSettings()
        {
            PresetsViewModel.SaveLastSelectedPreset();
            TracingViewModel.SaveLastSettings();
        }

        private void ApplyPreset()
        {
            try
            {
                if (cleaningSession.SelectedPreset != null)
                {
                    this.chamberDevice.SetLaserParameters(cleaningSession.SelectedPreset);

                    if (!IsCalibrating)
                        ApplyCleaningParameters();
                }
            }
            catch { }
        }

        private void ApplyCleaningParameters()
        {
            if (cleaningSession.SelectedPreset != null)
                this.chamberDevice.SetCleaningParameters(cleaningSession.SelectedPreset);
        }

        public void Receive(NeedRecalculateTraceMessage message)
        {
            ApplyPreset();
        }
    }
}
