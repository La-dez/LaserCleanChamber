using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.LaserComm
{
    public static class ModbusRtuHelper
    {
        // Коды функций
        public const byte FUNC_READ_HOLDING = 0x03;
        public const byte FUNC_WRITE_SINGLE = 0x06;

        /// <summary>
        /// Формирует запрос на чтение регистров (0x03)
        /// </summary>
        public static byte[] BuildReadRequest(byte slaveId, ushort startAddress, ushort count)
        {
            byte[] frame = new byte[8];
            frame[0] = slaveId;
            frame[1] = FUNC_READ_HOLDING;
            frame[2] = (byte)(startAddress >> 8);   // Адрес Hi
            frame[3] = (byte)(startAddress & 0xFF); // Адрес Lo
            frame[4] = (byte)(count >> 8);          // Кол-во регистров Hi
            frame[5] = (byte)(count & 0xFF);        // Кол-во регистров Lo

            AddCrc(frame);
            return frame;
        }

        /// <summary>
        /// Формирует запрос на запись одного регистра (0x06)
        /// </summary>
        public static byte[] BuildWriteSingleRequest(byte slaveId, ushort address, ushort value)
        {
            byte[] frame = new byte[8];
            frame[0] = slaveId;
            frame[1] = FUNC_WRITE_SINGLE;
            frame[2] = (byte)(address >> 8);   // Адрес Hi
            frame[3] = (byte)(address & 0xFF); // Адрес Lo
            frame[4] = (byte)(value >> 8);     // Значение Hi
            frame[5] = (byte)(value & 0xFF);   // Значение Lo

            AddCrc(frame);
            return frame;
        }

        /// <summary>
        /// Проверяет ответ и извлекает прочитанные значения (ushort)
        /// </summary>
        public static ushort[] ParseReadResponse(byte[] response, byte expectedSlaveId)
        {
            if (response == null || response.Length < 5)
                throw new Exception("Ответ слишком короткий.");

            if (!CheckCrc(response))
                throw new Exception("Ошибка CRC в ответе Modbus.");

            if (response[0] != expectedSlaveId)
                throw new Exception("Ответ от другого устройства (Slave ID не совпадает).");

            // Если старший бит функции установлен (например 0x83 вместо 0x03) — это ошибка Modbus
            if ((response[1] & 0x80) != 0)
                throw new Exception($"Ошибка Modbus (Exception Code: {response[2]})");

            byte byteCount = response[2];
            int registerCount = byteCount / 2;
            ushort[] values = new ushort[registerCount];

            for (int i = 0; i < registerCount; i++)
            {
                // Данные начинаются с 3-го байта, Hi-байт идет первым
                values[i] = (ushort)((response[3 + (i * 2)] << 8) | response[4 + (i * 2)]);
            }

            return values;
        }

        // --- Логика CRC-16 (Modbus) ---
        private static void AddCrc(byte[] frame)
        {
            ushort crc = CalculateCrc(frame, frame.Length - 2);
            // В Modbus RTU сначала идет младший байт CRC, затем старший
            frame[frame.Length - 2] = (byte)(crc & 0xFF);
            frame[frame.Length - 1] = (byte)(crc >> 8);
        }

        public static bool CheckCrc(byte[] frame)
        {
            if (frame.Length < 4) return false;
            ushort calculatedCrc = CalculateCrc(frame, frame.Length - 2);
            byte crcLo = (byte)(calculatedCrc & 0xFF);
            byte crcHi = (byte)(calculatedCrc >> 8);
            return frame[frame.Length - 2] == crcLo && frame[frame.Length - 1] == crcHi;
        }

        private static ushort CalculateCrc(byte[] data, int length)
        {
            ushort crc = 0xFFFF;
            for (int pos = 0; pos < length; pos++)
            {
                crc ^= data[pos];
                for (int i = 8; i != 0; i--)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }
    }
}
