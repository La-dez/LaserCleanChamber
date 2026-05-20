using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.LaserComm
{
    public static class LaserLimits
    {
        // Словарь: Ключ = Адрес регистра, Значение = Объект с лимитами
        public static readonly Dictionary<ushort, LaserParameter> Map = new()
        {
            // --- Параметры сканатора (оптической головки) ---
            { LaserRegisters.SwingWidth, new LaserParameter("Ширина колебаний", 0, 8, 2, "мм") },
            { LaserRegisters.SwingSpeed, new LaserParameter("Скорость колебаний", 1, 12000, 300, "мм/с") },
            { LaserRegisters.WeldingCenter, new LaserParameter("Центр сварки", -5, 5, 0, "мм") },

            // --- Параметры подачи проволоки ---
            { LaserRegisters.WireRetractSpeed, new LaserParameter("Скорость возврата проволоки", 3, 60, 15, "мм/с") },
            { LaserRegisters.WireFeedSpeed, new LaserParameter("Скорость подачи проволоки", 3, 60, 15, "мм/с") },
            // Примечание: для длин в документации нет жесткого лимита, берем безопасный диапазон 0-100 мм
            { LaserRegisters.WireRetractLength, new LaserParameter("Длина возврата проволоки", 0, 100, 5, "мм") },
            { LaserRegisters.WireFeedLength, new LaserParameter("Длина подачи проволоки", 0, 100, 5, "мм") },
            { LaserRegisters.AutoWireRetract, new LaserParameter("Авто-возврат проволоки", 0, 100, 0, "мм") },
            { LaserRegisters.WireCompensateLength, new LaserParameter("Длина досылки проволоки", 0, 100, 0, "мм") },

            { LaserRegisters.ManualWireFeedSpeed, new LaserParameter("Скорость ручной подачи", 3, 60, 15, "мм/с") },
            { LaserRegisters.ManualWireFeedLength, new LaserParameter("Длина ручной подачи", 0, 100, 10, "мм") },

            // --- Параметры мощности и лазера ---
            // ВНИМАНИЕ: Мин/Макс выходной мощности зависят от регистра MaxPower (10% - 100%). 
            // Здесь указан максимально широкий физический лимит.
            { LaserRegisters.LaserPowerOutput, new LaserParameter("Выходная мощность лазера", 0, 10000, 1000, "Вт") },
            { LaserRegisters.InitialPower, new LaserParameter("Начальная мощность", 0, 10000, 1000, "Вт") },
            { LaserRegisters.MaxPower, new LaserParameter("Максимальная мощность аппарата", 100, 10000, 2000, "Вт") },
            { LaserRegisters.WireCutPower, new LaserParameter("Мощность прожигания проволоки", 0, 10000, 1000, "Вт") },
        
            // 1000 = 100.0%, 1 = 0.1%
            { LaserRegisters.PwmDutyCycle, new LaserParameter("Скважность ШИМ", 1, 1000, 1000, "0.1%") },
            // Обычно частота ШИМ для таких лазеров от 50 до 50000 Гц
            { LaserRegisters.PwmFrequency, new LaserParameter("Частота ШИМ", 50, 30000, 2000, "Гц") },

            // --- Временные задержки (Тайминги) ---
            // Задержка возврата: в док-ции 0.1~10 сек. В Modbus передаются целые числа, поэтому 1~100 (х0.1 сек)
            { LaserRegisters.WireRetractDelay, new LaserParameter("Задержка возврата проволоки", 1, 100, 5, "0.1 с") },
            { LaserRegisters.PowerRiseTime, new LaserParameter("Время нарастания мощности", 1, 10000, 100, "мс") },
            { LaserRegisters.PowerFallTime, new LaserParameter("Время спада мощности", 1, 10000, 100, "мс") },
            { LaserRegisters.LaserOnDelay, new LaserParameter("Задержка включения излучения", 1, 10000, 150, "мс") },
            { LaserRegisters.LaserOffDelay, new LaserParameter("Задержка выключения излучения", 1, 10000, 150, "мс") },
            { LaserRegisters.GasOffDelay, new LaserParameter("Задержка отключения газа", 1, 10000, 200, "мс") },

            // --- Параметры точечной сварки (Spot Welding) ---
            { LaserRegisters.SpotWeldingTime, new LaserParameter("Время излучения (Точечная)", 1, 10000, 500, "мс") },
            { LaserRegisters.SpotWeldingPause, new LaserParameter("Пауза излучения (Точечная)", 1, 10000, 500, "мс") },
            { LaserRegisters.SpotWireFeedTime, new LaserParameter("Время подачи пров. (Точечная)", 1, 10000, 500, "мс") },
            { LaserRegisters.SpotWireStopTime, new LaserParameter("Время остановки пров. (Точечная)", 1, 10000, 500, "мс") },

            // --- Параметры двигателя и специфичные коррекции ---
            // Коэффициенты обычно не имеют строгих границ, ставим разумные пределы для short
            { LaserRegisters.MotorSpeedRatio, new LaserParameter("Коэфф. скорости двигателя", 0, 30000, 3200, "") },
            { LaserRegisters.MotorSpeedBias, new LaserParameter("Смещение скорости двигателя", -5000, 5000, -195, "") },
            { LaserRegisters.MeltWeldingCorrection, new LaserParameter("Коррекция сварки плавлением", -5, 5, 0, "мм") },
        };

        /// <summary>
        /// Безопасное получение параметра по адресу регистра.
        /// Возвращает null, если это системный параметр или он доступен только для чтения/перечисления.
        /// </summary>
        public static LaserParameter? Get(ushort registerAddress)
        {
            return Map.TryGetValue(registerAddress, out var param) ? param : null;
        }
    }
}
