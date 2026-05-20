using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.Communication
{
    public class Frame
    {
        private static CRC8 crc8 = new CRC8(0x31, 0xFF);
        private const int headerSize = 4;

        public byte[] Payload { get; private set; } = Array.Empty<byte>();
        public byte StartByte { get; } = 0xAA;
        public int PayloadLength { get => Payload.Length; }
        public byte MessageType { get; } = 0;
        public int FrameLength => PayloadLength + headerSize;

        public Frame(byte messageType, int payloadLength = 0)
        {
            MessageType = messageType;
            Payload = new byte[payloadLength];
        }

        public Frame(byte messageType, byte[] payload)
        {
            this.MessageType = messageType;

            this.Payload = new byte[payload.Length];
            Array.Copy(payload, 0, this.Payload, 0, payload.Length);
        }

        public byte[] ToByteArray()
        {
            byte[] array = new byte[FrameLength];
            array[0] = StartByte;

            if (PayloadLength > 0)
                Array.Copy(Payload, 0, array, 3, PayloadLength);

            array[1] = (byte)PayloadLength;
            array[2] = MessageType;

            array[3 + PayloadLength] = crc8.Calculate(array, 0, array.Length - 1);

            return array;
        }

        public byte CalculateCheckSum()
        {
            return this.ToByteArray().Last();
        }
    }

}
