using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LaserCleanChamber.Model;
using LaserCleanChamber.Model.LaserComm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LaserCleanChamber.ViewModel.MvvmMessages;

namespace LaserCleanChamber.ViewModel
{
    public partial class PresetsViewModel : ObservableObject
    {
        private readonly CleaningSession cleaningSession;

        [ObservableProperty]
        private double minPower = LaserLimits.Get<short>(LaserRegisters.LaserPowerOutput)?.MinValue ?? 0;

        [ObservableProperty]
        private double maxPower = LaserLimits.Get<short>(LaserRegisters.LaserPowerOutput)?.MaxValue ?? 0;

        [ObservableProperty]
        private double minScanWidth = LaserLimits.Get<short>(LaserRegisters.SwingWidth)?.MinValue ?? 0;

        [ObservableProperty]
        private double maxScanWidth = LaserLimits.Get<short>(LaserRegisters.SwingWidth)?.MaxValue ?? 0;

        [ObservableProperty]
        private double minScanSpeed = LaserLimits.Get<ushort>(LaserRegisters.SwingSpeed)?.MinValue ?? 0;

        [ObservableProperty]
        private double maxScanSpeed = LaserLimits.Get<ushort>(LaserRegisters.SwingSpeed)?.MaxValue ?? 0;

        public ObservableCollection<LaserPreset> Presets { get; set; } = new ObservableCollection<LaserPreset>();

        public LaserPreset? SelectedPreset
        {
            get => cleaningSession.SelectedPreset;
            set
            {
                cleaningSession.SelectedPreset = value;
                NotifyPresetChanged();
            }
        }

        private void NotifyPresetChanged()
        {
            OnPropertyChanged(nameof(SelectedPreset));
            WeakReferenceMessenger.Default.Send(new NeedRecalculateTraceMessage());
        }

        public PresetsViewModel(CleaningSession cleaningSession)
        {
            this.cleaningSession = cleaningSession;
            LoadPresets();
        }

        [RelayCommand]
        private void ApplyChanges()
        {
            try
            {
                PresetService.Save(Presets);
                NotifyPresetChanged();
            }
            catch (Exception ex) { }
        }

        [RelayCommand]
        private void DiscardChanges()
        {
            try
            {
                LoadPresets();
            }
            catch (Exception ex) { }
        }

        private void LoadPresets()
        {
            SelectedPreset = null;
            string lastPreset = Settings.Default.LastSelectedPreset;

            var presets = PresetService.Load();

            Presets.Clear();
            foreach (var preset in presets)
            {
                Presets.Add(preset);

                if (preset.Name == lastPreset)
                    SelectedPreset = preset;
            }

            if (Presets.Any() && SelectedPreset == null)
                SelectedPreset = Presets.First();
        }

        [RelayCommand]
        private void AddPresetCommand()
        {
            try
            {
                int count = Presets.Count;
                var preset = PresetService.GetDefaultPreset($"Режим {count + 1}");

                this.Presets.Add(preset);

                SelectedPreset = preset;
            }
            catch { }
        }

        [RelayCommand]
        private void RemovePresetCommand()
        {
            try
            {
                if (SelectedPreset != null)
                {
                    int lastSelectedIndex = this.Presets.IndexOf(SelectedPreset);

                    this.Presets.Remove(SelectedPreset);

                    if (lastSelectedIndex >= this.Presets.Count)
                        lastSelectedIndex -= 1;
                    if (lastSelectedIndex >= 0)
                        this.SelectedPreset = this.Presets[lastSelectedIndex];
                }
            }
            catch { }
        }

        public void SaveLastSelectedPreset()
        {
            Settings.Default.LastSelectedPreset = cleaningSession.SelectedPreset != null ? cleaningSession.SelectedPreset.Name : "";
        }
    }
}
