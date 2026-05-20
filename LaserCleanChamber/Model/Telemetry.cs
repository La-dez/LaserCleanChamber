using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model
{
    public record class Telemetry(
        bool IsError,
        bool DoorClosed,
        bool PlatePlaced,
        bool PistolPlaced,
        bool IsCleaning,
        double TemperatureInside_degC
        )
    {
    }
}
