using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.LaserComm
{
    using System;
    using System.Collections.Generic;

    public class ModbusRtuParser
    {
        private enum ParseState
        {
            WaitSlaveId,
            WaitFunctionCode,
            WaitByteCount,  // Только для функции 0x03
            ReadPayload
        }

        private ParseState _state = ParseState.WaitSlaveId;
        private readonly byte _expectedSlaveId;
        private readonly List<byte> _buffer = new List<byte>(256);
        private int _expectedLength = 0;

        public ModbusRtuParser(byte expectedSlaveId = 1)
        {
            _expectedSlaveId = expectedSlaveId;
        }

        /// <summary>
        /// Сброс машины состояний (вызывать перед каждым новым запросом и по таймауту)
        /// </summary>
        public void Reset()
        {
            _state = ParseState.WaitSlaveId;
            _buffer.Clear();
            _expectedLength = 0;
        }

        /// <summary>
        /// Возвращает собранный валидный фрейм
        /// </summary>
        public byte[] GetPacket() => _buffer.ToArray();

        /// <summary>
        /// Обработка входящего потока побайтово. 
        /// Возвращает true, когда валидный пакет полностью собран.
        /// </summary>
        public bool Process(byte b)
        {
            _buffer.Add(b);

            switch (_state)
            {
                case ParseState.WaitSlaveId:
                    if (b == _expectedSlaveId)
                    {
                        _state = ParseState.WaitFunctionCode;
                    }
                    else
                    {
                        // Если пришел мусор, сбрасываем, ждем ID
                        _buffer.Clear();
                    }
                    break;

                case ParseState.WaitFunctionCode:
                    if (b == ModbusRtuHelper.FUNC_READ_HOLDING) // 0x03
                    {
                        _state = ParseState.WaitByteCount; // Нам нужен еще один байт, чтобы узнать длину
                    }
                    else if (b == ModbusRtuHelper.FUNC_WRITE_SINGLE) // 0x06
                    {
                        _expectedLength = 8; // Ответ на 0x06 всегда 8 байт
                        _state = ParseState.ReadPayload;
                    }
                    else if ((b & 0x80) != 0) // Флаг ошибки Modbus (например 0x83 или 0x86)
                    {
                        _expectedLength = 5; // Пакет с ошибкой всегда 5 байт
                        _state = ParseState.ReadPayload;
                    }
                    else
                    {
                        // Неизвестная функция (мусор в линии). Сбрасываем.
                        Reset();
                    }
                    break;

                case ParseState.WaitByteCount:
                    // Для 0x03: текущий байт (b) — это количество байт данных.
                    // Длина = 1(ID) + 1(Func) + 1(ByteCount) + b(Данные) + 2(CRC) = b + 5
                    _expectedLength = b + 5;
                    _state = ParseState.ReadPayload;
                    break;

                case ParseState.ReadPayload:
                    if (_buffer.Count == _expectedLength)
                    {
                        // Пакет собран! Проверяем CRC
                        byte[] packet = _buffer.ToArray();
                        if (ModbusRtuHelper.CheckCrc(packet))
                        {
                            return true; // Пакет валиден и готов к чтению
                        }
                        else
                        {
                            // CRC не совпал. Сбрасываем.
                            Reset();
                        }
                    }
                    break;
            }

            return false;
        }
    }
}
