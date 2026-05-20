using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.LaserComm
{
    public interface ILaserParameter
    {
        string Name { get; }
        string Unit { get; }
        Type ValueType { get; }
    }

    /// <summary>
    /// Описание лимитов и свойств регистра лазера
    /// </summary>
    public class LaserParameter<T> : ILaserParameter
    {
        public string Name { get; }
        public T MinValue { get; }
        public T MaxValue { get; }
        public T DefaultValue { get; }
        public string Unit { get; }
        public Type ValueType => typeof(T);

        public LaserParameter(string name, T min, T max, T def, string unit)
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
        public bool IsValid(T value)
        {
            return Comparer<T>.Default.Compare(value, MinValue) >= 0
                && Comparer<T>.Default.Compare(value, MaxValue) <= 0;
        }

        /// <summary>
        /// Конвертирует привычное число (например, -5) в формат Modbus (ushort)
        /// </summary>
        public ushort ToModbusValue(T value)
        {
            if (!IsValid(value))
                throw new ArgumentOutOfRangeException(Name, $"Значение {value} {Unit} вне диапазона [{MinValue}...{MaxValue}]");

            if (typeof(T) == typeof(short))
            {
                return unchecked((ushort)(short)(object)value!);
            }

            if (typeof(T) == typeof(ushort))
            {
                return (ushort)(object)value!;
            }

            return Convert.ToUInt16(value);
        }

        /// <summary>
        /// Конвертирует сырое значение из Modbus обратно в знаковое число
        /// </summary>
        public T FromModbusValue(ushort modbusValue)
        {
            if (typeof(T) == typeof(short))
            {
                return (T)(object)unchecked((short)modbusValue);
            }

            if (typeof(T) == typeof(ushort))
            {
                return (T)(object)modbusValue;
            }

            return (T)Convert.ChangeType(modbusValue, typeof(T));
        }
    }
}
