using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace LaserCleanChamber.Model.LaserComm
{
    public class LaserPortManager : IDisposable
    {
        private readonly SerialPort _serialPort;
        private readonly ModbusRtuParser _parser;
        private readonly object _syncLock = new object(); // Защита от одновременных вызовов

        public bool IsOpened => _serialPort != null && _serialPort.IsOpen;

        public LaserPortManager(string portName, byte slaveId = 1)
        {
            _serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
            //_serialPort.RtsEnable = true;
            //_serialPort.DtrEnable = true;
            _serialPort.ReadTimeout = 1000; // Таймаут чтения одного байта
            _parser = new ModbusRtuParser(slaveId);
        }

        public void Open()
        {
            if (!_serialPort.IsOpen)
                _serialPort.Open();
        }

        public void Close()
        {
            if (_serialPort.IsOpen)
                _serialPort.Close();
        }

        /// <summary>
        /// Блокирующий метод: отправляет запрос и ждет ответа
        /// </summary>
        /// <param name="requestFrame">Сформированный массив байт запроса</param>
        /// <param name="timeoutMs">Общий таймаут ожидания ответа в мс</param>
        /// <returns>Валидный ответ (массив байт)</returns>
        public byte[] SendRequestAndWaitResponse(byte[] requestFrame, int timeoutMs = 500)
        {
            lock (_syncLock)
            {
                // 1. Подготовка: очищаем мусор в порту и сбрасываем парсер
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
                _parser.Reset();

                // 2. Отправляем запрос
                _serialPort.Write(requestFrame, 0, requestFrame.Length);

                // 3. Ждем ответ
                DateTime startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMs)
                {
                    if (_serialPort.BytesToRead > 0)
                    {
                        // Читаем по одному байту (можно читать блоками, но для машины состояний побайтово нагляднее)
                        byte b = (byte)_serialPort.ReadByte();

                        // Скармливаем байт парсеру
                        if (_parser.Process(b))
                        {
                            // Пакет успешно собран!
                            return _parser.GetPacket();
                        }
                    }
                    else
                    {
                        // Если данных пока нет, отдаем квант времени, чтобы не грузить процессор
                        Thread.Sleep(1);
                    }
                }

                throw new TimeoutException("Таймаут ожидания ответа от лазера.");
            }
        }

        public void Dispose()
        {
            Close();
            _serialPort?.Dispose();
        }
    }
}
