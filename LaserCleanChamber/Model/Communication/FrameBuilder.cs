using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.Communication
{
    public class FrameBuilder
    {
        private enum State
        {
            WaitingForStart, // Ждем 0xAA
            ReadingLength,   // Читаем длину данных
            ReadingMsgType,  // Читаем тип сообщения
            ReadingPayload,  // Читаем само тело сообщения
            ReadingCRC,      // Читаем контрольную сумму
        }

        private State currentState = State.WaitingForStart;
        private int expectedLength;
        private byte msgType;
        private byte[] buffer = Array.Empty<byte>();
        private int payloadIndex;

        public bool ProcessByte(byte b, out Frame? frame)
        {
            switch (currentState)
            {
                case State.WaitingForStart:
                    if (b == 0xAA)
                        currentState = State.ReadingLength;
                    break;

                case State.ReadingLength:
                    expectedLength = b;
                    buffer = new byte[expectedLength];
                    payloadIndex = 0;
                    currentState = State.ReadingMsgType;
                    break;

                case State.ReadingMsgType:
                    msgType = b;

                    if (expectedLength == 0)
                        currentState = State.ReadingCRC;
                    else
                        currentState = State.ReadingPayload;
                    
                    break;

                case State.ReadingPayload:
                    buffer[payloadIndex] = b;
                    payloadIndex++;

                    if (payloadIndex >= expectedLength)
                    {
                        currentState = State.ReadingCRC;
                    }
                    break;

                case State.ReadingCRC:
                    byte receivedCrc = b;

                    frame = new Frame(msgType, buffer);
                    return true;
                    /*if (receivedCrc == frame.CheckSum)
                    {
                        return true;
                    }
                    else
                    {
                        throw new Exception($"CRC Error. Expected: {frame.CheckSum:X2}, Got: {receivedCrc:X2}");
                    }*/
            }

            frame = null;
            return false;
        }

        public void Reset()
        {
            currentState = State.WaitingForStart;
            expectedLength = 0;
            msgType = 0;
            buffer = Array.Empty<byte>();
            payloadIndex = 0;
        }

        public static Frame BuildFromBytesArray(byte[] rawBuffer)
        {
            FrameBuilder frameBuilder = new FrameBuilder();
            Frame? frame = null;
            for (int i = 0; i < rawBuffer.Length; i++)
            {
                if(frameBuilder.ProcessByte(rawBuffer[i], out frame))
                {
                    if (frame != null)
                        return frame;
                    else
                        break;
                }
            }
            throw new Exception("Can not build frame");
        }
    }
}
