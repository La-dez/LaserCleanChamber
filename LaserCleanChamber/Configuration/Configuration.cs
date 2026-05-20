using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Configuration
{
    public class AppSettings
    {
        // Группа настроек калибровки
        public CalibrationSettings Calibration { get; set; } = new CalibrationSettings();

        // Задел на будущее: Группа настроек лазера (например, порты подключения)
        public HardwareSettings Hardware { get; set; } = new HardwareSettings();

        // Задел на будущее: Настройки интерфейса (язык, тема)
        public UiSettings UI { get; set; } = new UiSettings();

        public TracingSettings Tracing { get; set; } = new TracingSettings();
    }

    // Класс только для калибровок осей
    public class CalibrationSettings
    {
        public double StepsPerMmX { get; set; } = 17500.0 / 208.0; //Lx = 218 mm Шагов = 17500
        public double StepsPerMmY { get; set; } = 17500.0 / 225.0; //Ly = 218 mm Шагов = 17500
        public double StepsPerMmZ { get; set; } = 20000.0 / 78.0; //Lz = 78 mm Шагов = 20000

        public double CenterOffsetX { get; set; } = 10000; // середина оси X
        public double CenterOffsetY { get; set; } = 9000; // Середина оси Y
        public double CenterOffsetZ { get; set; } = 10000;  // Фокусное расстояние по Z
    }
    
    public class TracingSettings
    {
        public string ModelFileName { get; set; } = "Mesh/Plate_v1.stl";
        public string Algorythm { get; set; } = "SnakeModif";
        public double Overlap { get; set; } = -125;
        public double TraceStep { get; set; } = 1;
        public double ApproxError { get; set; } = 3;
        public double Margin { get; set; } = 0;
    }

    public class HardwareSettings
    {
        //public string ComPort { get; set; } = "COM3";
        public int BaudRate { get; set; } = 115200;
        public string VideoDeviceName { get; set; } = "CCX2F3299";
        public bool UseHardwareEmulator { get; set; } = true;
    }

    public class UiSettings
    {
        public bool IsDarkMode { get; set; } = true;
    }
}
