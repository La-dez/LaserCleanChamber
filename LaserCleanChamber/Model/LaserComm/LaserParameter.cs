using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.LaserComm
{
    /// <summary>
    /// Описание лимитов и свойств регистра лазера
    /// </summary>
    public class LaserParameter
    {
        public string Name { get; }
        public short MinValue { get; }
        public short MaxValue { get; }
        public short DefaultValue { get; }
        public string Unit { get; }

        public LaserParameter(string name, short min, short max, short def, string unit)
        {
            Name = name;
            MinValue = min;
            MaxValue = max;
            DefaultValue = def;
            Unit = unit;
        }

        /// <summary>
        /// Проверка, входит ли значение в допустимый диапазон
        /// </summary>
        public bool IsValid(short value)
        {
            return value >= MinValue && value <= MaxValue;
        }

        /// <summary>
        /// Конвертирует привычное число (например, -5) в формат Modbus (ushort)
        /// </summary>
        public ushort ToModbusValue(short value)
        {
            if (!IsValid(value))
                throw new ArgumentOutOfRangeException(Name, $"Значение {value} {Unit} вне диапазона [{MinValue}...{MaxValue}]");

            // Прямое приведение short к ushort автоматически делает Two's complement
            // Например: -5 превратится в 65531
            return (ushort)value;
        }

        /// <summary>
        /// Конвертирует сырое значение из Modbus обратно в знаковое число
        /// </summary>
        public short FromModbusValue(ushort modbusValue)
        {
            return (short)modbusValue;
        }
    }
}
