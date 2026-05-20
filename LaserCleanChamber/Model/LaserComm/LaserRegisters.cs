using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.LaserComm
{
    using System;

    /// <summary>
    /// Карта регистров Modbus для ручного лазерного сварочного аппарата (Holding Registers 4x)
    /// </summary>
    public static class LaserRegisters
    {
        #region Базовые параметры (0 - 42)

        // [СИСТЕМНЫЙ ПАРАМЕТР] Центр колебаний сканатора (по умолчанию 8192)
        public const ushort SwingCenter = 0;

        public const ushort SwingWidth = 1;          // Ширина колебаний (0~8 мм)
        public const ushort SwingSpeed = 2;          // Скорость колебаний (1~1200 мм/с)

        // [СИСТЕМНЫЙ ПАРАМЕТР] Режим колебаний сканатора
        public const ushort SwingMode = 3;

        public const ushort WeldingCenter = 4;       // Центр сварки (-5~+5 мм)

        // [СИСТЕМНЫЙ ПАРАМЕТР] Коррекция ширины сканатора
        public const ushort SwingWidthCorrection = 5;

        public const ushort WireRetractSpeed = 6;    // Скорость возврата проволоки (3~60 мм/с)
        public const ushort WireFeedSpeed = 7;       // Скорость подачи проволоки (3~60 мм/с)
        public const ushort WireRetractLength = 8;   // Длина возврата проволоки (мм)
        public const ushort WireFeedLength = 9;      // Длина подачи проволоки (мм)
        public const ushort LaserPowerOutput = 10;   // Выходная мощность лазера (Вт)

        // [СИСТЕМНЫЙ ПАРАМЕТР] Коррекция мощности лазера
        public const ushort LaserPowerCorrection = 11;

        public const ushort PwmDutyCycle = 12;       // Скважность ШИМ (0.1~100.0%, 1000 = 100%)
        public const ushort WireRetractDelay = 13;   // Задержка возврата проволоки (0.1~10 с)
        public const ushort PwmFrequency = 14;       // Частота ШИМ (по умолчанию 2000)
        public const ushort PowerRiseTime = 15;      // Время нарастания мощности (1~10000 мс)
        public const ushort LaserOnDelay = 16;       // Задержка включения излучения (1~10000 мс)
        public const ushort GasOffDelay = 17;        // Задержка отключения газа (1~10000 мс)
        public const ushort LaserOffDelay = 18;      // Задержка выключения излучения (1~10000 мс)
        public const ushort InitialPower = 19;       // Начальная мощность (по умолчанию 1000)
        public const ushort MaxPower = 20;           // Макс. мощность (зависит от лазера, по ум. 2000)
        public const ushort AutoWireRetract = 21;    // Автоматический возврат проволоки (мм)
        public const ushort SpotWeldingTime = 22;    // Время излучения при точечной сварке (1~10000 мс)
        public const ushort SpotWeldingPause = 23;   // Пауза при точечной сварке (1~10000 мс)
        public const ushort WireCompensateLength = 24;// Длина досылки (компенсации) проволоки (мм)
        public const ushort PowerFallTime = 25;      // Время спада мощности (1~10000 мс)
        public const ushort MotorSpeedRatio = 26;    // Коэффициент скорости двигателя
        public const ushort MotorSpeedBias = 27;     // Смещение скорости двигателя
        public const ushort ManualWireFeedSpeed = 28; // Скорость ручной подачи проволоки (3~60 мм/с)
        public const ushort ManualWireFeedLength = 29;// Длина ручной подачи проволоки (мм)

        // [СИСТЕМНЫЕ ПАРАМЕТРЫ] (30 - 38)
        public const ushort MaxCurrent = 30;
        public const ushort MinCurrent = 31;
        public const ushort PidProportional = 32;
        public const ushort PidIntegral = 33;
        public const ushort PidDerivative = 34;
        public const ushort PidFeedForward = 35;
        public const ushort MeltWeldingCorrection = 36; // Коррекция сварки плавлением (-5~+5 мм)
        public const ushort CenterSpeed = 37;
        public const ushort Backlash = 38;           // Обратный зазор (люфт)

        public const ushort WireCutPower = 39;       // Мощность прожигания проволоки (Вт)
        public const ushort ActivationState = 40;    // Статус активации (0 = активировано)
        public const ushort SpotWireFeedTime = 41;   // Время подачи проволоки в точечном режиме (мс)
        public const ushort SpotWireStopTime = 42;   // Время остановки проволоки в точечном режиме (мс)

        public const ushort WeldingModeSelect = 50;  // Режим сварки (использовать enum WeldingMode)
        #endregion

        #region Специальные регистры (1000+)

        public const ushort CommandWord = 1001;      // Регистр команд (запись вызывает действие)
        public const ushort InputStatus = 1010;      // Слово состояния входов (Чтение)
        public const ushort OutputStatus = 1011;     // Слово состояния выходов (Чтение)
        public const ushort SystemControl = 1012;    // Слово управления системой (Чтение/Запись)

        #endregion
    }

    #region Перечисления (Enums) для значений регистров

    /// <summary>
    /// Режимы сварки (для регистра 50)
    /// </summary>
    public enum WeldingMode : ushort
    {
        Continuous = 0, // Непрерывный
        Spot = 1,       // Точечный
        FishScale = 2,  // Рыбья чешуя (импульсный шов)
        Cleaning = 3    // Режим очистки
    }

    /// <summary>
    /// Команды управления (для записи в регистр 1001)
    /// </summary>
    public enum LaserCommand : ushort
    {
        SaveParameters = 1,      // Сохранить параметры
        LoadParameters = 2,      // Загрузить параметры
        Activate = 3,            // Активировать
        ManualWireFeed = 4,      // Ручная подача проволоки
        ManualWireRetract = 5,   // Ручной возврат проволоки
        SaveProcessParams = 8,   // Сохранить технологические параметры
        LoadProcessParams = 9,   // Загрузить технологические параметры
        ResetParameters = 21     // Сброс параметров к заводским
    }

    /// <summary>
    /// Битовая маска состояния входов (Регистр 1010)
    /// </summary>
    [Flags]
    public enum InputStatusBits : ushort
    {
        None = 0,
        SafetyLock = 1 << 0,       // Bit0: Замок безопасности
        LaserTrigger = 1 << 1,     // Bit1: Курок лазера
        LaserAlarm = 1 << 2,       // Bit2: Авария лазера
        ChillerAlarm = 1 << 3,     // Bit3: Авария водяного охлаждения
        GasPressureAlarm = 1 << 4  // Bit4: Авария давления газа
    }

    /// <summary>
    /// Битовая маска состояния выходов (Регистр 1011)
    /// </summary>
    [Flags]
    public enum OutputStatusBits : ushort
    {
        None = 0,
        GasValve = 1 << 5,         // Bit5: Газовый клапан
        LaserEnable = 1 << 6,      // Bit6: Разрешение работы лазера (Enable)
        ExternalWireFeed = 1 << 10,// Bit10: Внешняя подача проволоки
        RedPointer = 1 << 11       // Bit11: Красный указывающий луч (пилот)
    }

    /// <summary>
    /// Битовая маска управления системой (Регистр 1012)
    /// </summary>
    [Flags]
    public enum SystemControlBits : ushort
    {
        None = 0,
        DisableWireFeed = 1 << 0,       // Bit0: 0-Подача вкл, 1-Подача выкл
        SpotWeldingMode = 1 << 1,       // Bit1: 0-Непрерывный, 1-Точечный
        DisableMotorSwing = 1 << 2,     // Bit2: 0-Колебания вкл, 1-Колебания выкл
        ManualFeedSpeedMode = 1 << 3,   // Bit3: 0-По длине, 1-По скорости
        ChillerAlarmNO = 1 << 4,        // Bit4: Полярность чиллера (0-NC, 1-NO)
        GasAlarmNO = 1 << 5,            // Bit5: Полярность газа (0-NC, 1-NO)
        LaserAlarmNO = 1 << 6,          // Bit6: Полярность лазера (0-NC, 1-NO)
        WireFeedReverse = 1 << 7,       // Bit7: Направление подачи (0-Вперед, 1-Назад)
        ExternalWireFeedLED = 1 << 13,  // Bit13: Индикация внешней подачи (1-Вкл)
        FishScaleMode = 1 << 14         // Bit14: Режим "Рыбья чешуя" (1-Вкл)
    }

    #endregion
}
