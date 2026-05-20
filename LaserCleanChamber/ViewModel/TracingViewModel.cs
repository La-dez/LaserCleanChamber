using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LaserCleanChamber.Model;
using LaserCleanChamber.Model.Slicing;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.Pkcs;
using System.Windows;
using static LaserCleanChamber.ViewModel.MvvmMessages;

namespace LaserCleanChamber.ViewModel
{

    public partial class TracingViewModel : ObservableObject, IRecipient<NeedRecalculateTraceMessage>
    {
        private readonly CleaningSession cleaningSession;

        public IEnumerable<TracingAlgorithm> TracingAlgorithms => Enum.GetValues<TracingAlgorithm>().Cast<TracingAlgorithm>();

        public TracingAlgorithm TracingAlgorythm
        {
            get => cleaningSession.TracingAlgorithm;
            set
            {
                cleaningSession.TracingAlgorithm = value;
                RecalculateAndRedraw();
                OnPropertyChanged();
            }
        }

        public IEnumerable<PlateSides> PlateSides => Enum.GetValues<PlateSides>().Cast<PlateSides>();
        public PlateSides PlateSide
        {
            get => cleaningSession.PlateSide;
            set
            {
                cleaningSession.PlateSide = value;
                RecalculateAndRedraw();
                OnPropertyChanged();
            }
        }

        public GeometryModel? Model => cleaningSession.GetModel3D();
        //public List<g3.Vector3d>? Trace => cleaningSession.Trajectory;
        public List<PathSegment<g3.Vector3d>>? Trace => cleaningSession.Trajectory;

        public Rect TracingRegion
        {
            get => cleaningSession.ROI;
            set
            {
                cleaningSession.ROI = value;
                RecalculateAndRedraw();
                OnPropertyChanged();
            }
        }

        public TracingViewModel(CleaningSession cleaningSession)
        {
            this.cleaningSession = cleaningSession;

            WeakReferenceMessenger.Default.Register(this);

            try
            {
                if(Enum.TryParse(Settings.Default.LastSelectedTracingAlgorythm, true, out TracingAlgorithm algorithm))
                    TracingAlgorythm = algorithm;
                
                if (Enum.TryParse(Settings.Default.LastSelectedSide, true, out PlateSides side))
                    PlateSide = side;
            }
            catch { }
        }

        public void Receive(NeedRecalculateTraceMessage message)
        {
            RecalculateAndRedraw();
        }

        private void RecalculateAndRedraw()
        {
            try
            {
                cleaningSession.CalculateTrajectory();
                OnPropertyChanged(nameof(Trace));
                OnPropertyChanged(nameof(Model));
            }
            catch (Exception ex)
            {
            }
        }

        public void SaveLastSettings()
        {
            Settings.Default.LastSelectedTracingAlgorythm = this.TracingAlgorythm.ToString();
            Settings.Default.LastSelectedSide = this.PlateSide.ToString();
        }
    }
}