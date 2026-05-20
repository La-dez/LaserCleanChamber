using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaserCleanChamber.Configuration;
using LaserCleanChamber.Model.Communication;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.ViewModel
{
    public partial class ConnectionViewModel : ObservableObject
    {
        private ObservableCollection<ComPortInfo> comPortInfos = new ObservableCollection<ComPortInfo>();
        public ObservableCollection<ComPortInfo> ComPortInfos => comPortInfos;

        [ObservableProperty]
        private ComPortInfo? selectedChamberDevice = null;

        [ObservableProperty]
        private ComPortInfo? selectedLaserDevice = null;

        private AppSettings currentSettings;

        public ConnectionViewModel(AppSettings currentSettings)
        {
            this.currentSettings = currentSettings;
            Refresh();
            SelectLastDevices();
        }

        /*partial void OnSelectedChamberDeviceChanged(ComPortInfo? value)
        {
            if (value != null)
            {
                Settings.Default.LastSelectedChamberDevice = value.Description ?? "";
            }
        }

        partial void OnSelectedLaserDeviceChanged(ComPortInfo? value)
        {
            if (value != null)
            {
                Settings.Default.LastSelectedLaserDevice = value.Description ?? "";
            }
        }*/

        public void SaveLastDevices()
        {
            Settings.Default.LastSelectedLaserDevice = SelectedLaserDevice?.Description ?? "";
            Settings.Default.LastSelectedChamberDevice = SelectedChamberDevice?.Description ?? "";
        }

        void SelectLastDevices()
        {
            try
            {
                foreach (var comPortInfo in comPortInfos)
                {
                    if(comPortInfo.Description == Settings.Default.LastSelectedChamberDevice)
                    {
                        SelectedChamberDevice = comPortInfo;
                        break;
                    }
                }
                foreach (var comPortInfo in comPortInfos)
                {
                    if (comPortInfo.Description == Settings.Default.LastSelectedLaserDevice)
                    {
                        SelectedLaserDevice = comPortInfo;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        [RelayCommand]
        public void Refresh()
        {
            try
            {
                var ports = ComHelper.EnumerateComPorts();

                comPortInfos.Clear();
                foreach (var port in ports)
                    comPortInfos.Add(port);

                if(ports.Any())
                    SelectedLaserDevice = ports.Last();
                else
                    SelectedLaserDevice = null;

                if (ports.Any())
                    SelectedChamberDevice = ports.Last();
                else
                    SelectedChamberDevice = null;
            }
            catch { }
        }
    }
}
