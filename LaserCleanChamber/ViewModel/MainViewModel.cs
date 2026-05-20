using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaserCleanChamber.Configuration;
using LaserCleanChamber.Model;
using LaserCleanChamber.Model.Communication;
using SharpVectors.Renderers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LaserCleanChamber.ViewModel
{
    public partial class MainViewModel : ObservableObject, IDisposable
    {
        private AppSettings currentSettings = SettingsManager.Load();
        public AppSettings CurrentSettings => currentSettings;

        [ObservableProperty]
        private ConnectionViewModel connectionViewModel;

        [ObservableProperty]
        private ChamberViewModel? chamberViewModel = null;

        public bool IsConnected => ChamberViewModel != null;
        public bool IsHardwareEmulatorEnabled => currentSettings.Hardware.UseHardwareEmulator;

        public MainViewModel() {
            connectionViewModel = new ConnectionViewModel(currentSettings);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(ChamberViewModel))
            {
                OnPropertyChanged(nameof(IsConnected));
            }
        }

        [RelayCommand]
        public void ConnectMoc()
        {
            Disconnect();
            try
            {
                IChamberDevice chamberDevice = new MocChamberDevice();
                this.ChamberViewModel = new ChamberViewModel(chamberDevice, currentSettings);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось подключиться: {ex.Message}");
            }
        }

        [RelayCommand]
        public void Connect()
        {
            Disconnect();
            try
            {
                if (currentSettings.Hardware.UseHardwareEmulator)
                {
                    IChamberDevice chamberDevice = new MocChamberDevice();
                    this.ChamberViewModel = new ChamberViewModel(chamberDevice, currentSettings);
                    return;
                }

                if (ConnectionViewModel.SelectedChamberDevice == null ||
                    ConnectionViewModel.SelectedLaserDevice == null)
                {
                    MessageBox.Show("Не выбраны устройства для подключения.");
                    return;
                }

                SerialPortConnectionProperties connectionProperties = new SerialPortConnectionProperties(
                    PortName: ConnectionViewModel.SelectedChamberDevice.PortName);
                IChamberDevice realChamberDevice = new ChamberDevice(
                    connectionProperties,
                    ConnectionViewModel.SelectedLaserDevice.PortName,
                    currentSettings.Calibration);

                this.ChamberViewModel = new ChamberViewModel(realChamberDevice, currentSettings);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось подключиться: {ex.Message}");
            }
        }

        public void AutoConnectIfNeeded()
        {
            if (IsConnected || !currentSettings.Hardware.UseHardwareEmulator)
                return;

            Connect();
        }

        public void Disconnect()
        {
            try
            {
                ChamberViewModel?.SaveLastSettings();

                this.ChamberViewModel?.Dispose();
                this.ChamberViewModel = null;
            }
            catch { }
        }

        public void Dispose()
        {
            try
            {
                Disconnect();
            }
            catch { }

            ConnectionViewModel.SaveLastDevices();

            Settings.Default.Save();
        }
    }
}
