using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model
{
    public class MachineCalibration
    {
        // Масштаб: Сколько шагов мотора в 1 миллиметре (Steps per mm)
        public double StepsPerMmX { get; set; }
        public double StepsPerMmY { get; set; }
        public double StepsPerMmZ { get; set; }

        // Результаты калибровок по осям
        public bool IsXCalibrated { get; set; } = false;
        public bool IsYCalibrated { get; set; } = false;
        public bool IsZCalibrated { get; set; } = false;

        public bool IsAllCalibrated => IsXCalibrated && IsYCalibrated && IsZCalibrated;

        // Смещение: Координаты (в шагах), где физически находится центр 3D модели
        public double CenterOffsetX { get; set; }
        public double CenterOffsetY { get; set; }
        public double CenterOffsetZ { get; set; }

        // Лимиты станка (в шагах), чтобы лазер не врезался в стенки
        public uint MaxStepsX { get; set; } = 17500;
        public uint MaxStepsY { get; set; } = 17500;
        public uint MaxStepsZ { get; set; } = 20000;

        public double ToMachineX(double x_mm)
        {
            return Math.Clamp(CenterOffsetX + (x_mm * StepsPerMmX), 0, MaxStepsX - 1);
        }

        public double ToMachineY(double y_mm)
        {
            return Math.Clamp(CenterOffsetY + (y_mm * StepsPerMmY), 0, MaxStepsY - 1);
        }

        public double ToMachineZ(double z_mm)
        {
            return Math.Clamp(CenterOffsetZ + (z_mm * StepsPerMmZ), 0, MaxStepsZ - 1);
        }
    }
}
