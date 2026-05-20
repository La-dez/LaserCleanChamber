using LaserCleanChamber.Model.Slicing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LaserCleanChamber.Model
{
    public enum ChamberDeviceState : int
    {
        Idle = 0,
        Calibrating,
        Cleaning,
        Error
    }

    public interface IChamberDevice : IDisposable
    {
        event Action<ChamberDeviceState>? OnStateChanged;
        event Action<Telemetry>? OnTelemetryUpdated;
        event Action<string>? OnErrorMessage;
        event Action<bool>? OnCalibrationDone;

        ChamberDeviceState State { get; }
        Telemetry Telemetry { get; }
        bool IsCalibrated { get; }

        void StartCleaning(LaserPreset preset, List<PathSegment<g3.Vector3d>> trace);
        void StopCleaning();
        void StartCalibrating();

        void SetLaserParameters(LaserPreset preset);
    }
}
