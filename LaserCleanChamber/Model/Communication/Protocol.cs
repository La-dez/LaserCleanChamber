using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace LaserCleanChamber.Model.Communication
{
    public enum MessageType : byte
    {
        Unknown = 0,
        MSG_LASER_CLEAN = 0x01,
        MSG_SET_LASER_POWER = 0x02,
        MSG_SET_GALVO_WIDTH = 0x03,
        MSG_SET_GALVO_SPEED = 0x04,
        MSG_TELEMETRY = 0x10,
        STMP_CALIBRATION = 0xC0,
        STMP_MOVE_COORD = 0xC2,
        STMP_MOVE_ABS_COORD = 0xC3,
        STMP_SWITCH_OFF = 0xC6,
        SEND_TRAECTORY = 0xC7,
        MSG_LASER_CLEAN_SWITCH = 0xC8,
        MSG_SET_COOLDOWN_AND_REPEATS_CLEANING = 0xC9,
        MSG_SET_COOLDOWN_LINES = 0xCA,
    }

    public static class Protocol
    {
        private static bool isBitSet(byte b, int bit)
        {
            return (b & (1 << bit)) != 0;
        }

        public static Frame EncodeGetTelemetery()
        {
            return new Frame((byte)MessageType.MSG_TELEMETRY);
        }

        public static Telemetry DecodeTelemetry(Frame frame)
        {
            if (frame.PayloadLength < 3 || (MessageType)frame.MessageType != MessageType.MSG_TELEMETRY)
                throw new Exception("Telemetery decode error");

            Telemetry telemetry = new Telemetry(
                IsError: isBitSet(frame.Payload[0], 7),
                DoorClosed: !isBitSet(frame.Payload[0], 3),
                PlatePlaced: !isBitSet(frame.Payload[0], 5),
                PistolPlaced: !isBitSet(frame.Payload[0], 4),
                IsCleaning: frame.Payload[1] == 1,
                TemperatureInside_degC: 0 //TODO: ((frame.Payload[2] << 8) + frame.Payload[3]) / 10.0
                );

            return telemetry;
        }

        public struct SendTrajectoryResult
        {
            public byte readedBytes;
            public bool success;
        }

        public static SendTrajectoryResult DecodeSendTrajectoryResult(Frame responce)
        {
            return new SendTrajectoryResult()
            {
                readedBytes = responce.Payload[0],
                success = responce.Payload[1] == 0
            };
        }

        public static Frame EncodeLaserCleanSwitch(bool on)
        {
            return new Frame((byte)MessageType.MSG_LASER_CLEAN_SWITCH, [(byte)(on ? 1 : 0)]);
        }

        public static Frame EncodeSetCooldownAndRepeatsCleaning(int repeats, int waitSeconds)
        {
            repeats = Math.Clamp(repeats, 0, byte.MaxValue);
            waitSeconds = Math.Clamp(waitSeconds, 0, 600);

            List<byte> payload = new List<byte>(3)
            {
                (byte)repeats
            };
            SerializeUInt16MsbFirst((ushort)waitSeconds, payload);

            return new Frame((byte)MessageType.MSG_SET_COOLDOWN_AND_REPEATS_CLEANING, payload.ToArray());
        }

        public static Frame EncodeSetCooldownLines(int waitSeconds)
        {
            waitSeconds = Math.Clamp(waitSeconds, 0, 600);

            List<byte> payload = new List<byte>(2);
            SerializeUInt16MsbFirst((ushort)waitSeconds, payload);

            return new Frame((byte)MessageType.MSG_SET_COOLDOWN_LINES, payload.ToArray());
        }

        public static Frame EncodeLaserClean(bool cleaningOn)
        {
            byte b = (byte)(cleaningOn ? 1 : 0);
            return new Frame(
                (byte)MessageType.MSG_LASER_CLEAN, [b]);
        }

        public static Frame EncodeSendTraectoryPart(ushort startIndex, List<TracePoint> tracePart)
        {
            List<byte> binary = new List<byte>();
            SerializeUInt16MsbFirst(startIndex, binary);

            for (int i = 0; i < tracePart.Count; i++)
            {
                TracePoint p = tracePart[i];
                p.Serialize(binary);
            }
            return new Frame((byte)MessageType.SEND_TRAECTORY, binary.ToArray());
        }

        public const int LaserPowerMin = 10;
        public const int LaserPowerMax = 100;

        public static Frame EncodeSetLaserPower(int power)
        {
            power = Math.Clamp(power, LaserPowerMin, LaserPowerMax);
            return new Frame((byte)MessageType.MSG_SET_LASER_POWER, [(byte)power]);
        }

        public const int GalvoWidthMin = 20;
        public const int GalvoWidthMax = 100;

        public static Frame EncodeSetGalvoWidth(int galvo_width)
        {
            galvo_width = Math.Clamp(galvo_width, GalvoWidthMin, GalvoWidthMax);
            return new Frame((byte)MessageType.MSG_SET_GALVO_WIDTH, [(byte)galvo_width]);
        }

        public const int GalvoSpeedMin = 1;
        public const int GalvoSpeedMax = 1200;

        public static Frame EncodeSetGalvoSpeed(int galvo_speed)
        {
            galvo_speed = Math.Clamp(galvo_speed, GalvoSpeedMin, GalvoSpeedMax);
            return new Frame((byte)MessageType.MSG_SET_GALVO_SPEED, [(byte)galvo_speed]);
        }

        public enum MotorAxis : byte
        {
            X = 1, Y = 2, Z = 3,
        };

        public enum CoordAxis : byte
        {
            X = 3, Y = 4
        };

        public enum Direction : sbyte
        {
            Backward = 0,
            Forward = 1
        };

        public struct TracePoint
        {
            public uint X, Y, Z;
            public bool LaserOn;

            public int SizeInBytes => 3 * 4 + 1;
            public void Serialize(List<byte> binary)
            {
                SerializeUInt32MsbFirst(X, binary);
                SerializeUInt32MsbFirst(Y, binary);
                SerializeUInt32MsbFirst(Z, binary);
                binary.Add((byte)(LaserOn ? 1 : 0));
            }

            public override string ToString()
            {
                return $"X={X:F3} Y={Y:F3} Z={Z:F3} L{(LaserOn ? 1 : 0)}";
            }
        };

        public static Frame EncodeStmpCalibration(MotorAxis motorAxis)
        {
            return new Frame((byte)MessageType.STMP_CALIBRATION, [(byte)motorAxis]);
        }

        public static (MotorAxis motorAxis1, uint steps) DecodeStmpCalibration(Frame frame)
        {
            if ((MessageType)frame.MessageType != MessageType.STMP_CALIBRATION || frame.PayloadLength < 5)
                throw new Exception("Stmp calibration decode error");
            
            byte motorAxisByte = frame.Payload[0];
            if (!Enum.IsDefined(typeof(MotorAxis), motorAxisByte))
                throw new ArgumentOutOfRangeException($"Invalid MotorAxis value: {motorAxisByte}");
            
            MotorAxis motorAxis = (MotorAxis)motorAxisByte;
            uint steps = GetUInt32(frame.Payload, 1);

            return (motorAxis, steps);
        }

        public static Frame EncodeStmpMoveRelativeCoord(CoordAxis axis, Direction dir, uint steps)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using(BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write((byte)axis);
                    binaryWriter.Write((byte)dir);
                    binaryWriter.Write(UInt32ToArray(steps));
                }
                return new Frame((byte)MessageType.STMP_MOVE_COORD, memoryStream.ToArray());
            }
        }

        public static Frame EncodeStmpMoveAbsCoord(CoordAxis axis, uint abs_coord)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write((byte)axis);
                    binaryWriter.Write(UInt32ToArray(abs_coord));
                }
                return new Frame((byte)MessageType.STMP_MOVE_ABS_COORD, memoryStream.ToArray());
            }
        }

        public static Frame EncodeStmpSwitch(bool driverOn)
        {
            return new Frame((byte)MessageType.STMP_SWITCH_OFF, [(byte)(driverOn ? 1 : 0)]);
        }

        private static byte[] Int32ToArray(int value)
        {
            return
            [
                (byte)((value >> 24) & 0xFF),
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)( value & 0xFF)
            ];
        }

        private static byte[] UInt32ToArray(uint value)
        {
            return Int32ToArray(System.Convert.ToInt32(value));
        }

        private static void SerializeUInt32MsbFirst(uint value, List<byte> binary)
        {
            binary.Add((byte)((value >> 24) & 0xFF));
            binary.Add((byte)((value >> 16) & 0xFF));
            binary.Add((byte)((value >> 8) & 0xFF));
            binary.Add((byte)( value & 0xFF));
        }

        private static void SerializeInt32MsbFirst(int value, List<byte> binary)
        {
            SerializeUInt32MsbFirst((uint)value, binary);
        }

        private static void SerializeUInt16MsbFirst(ushort value, List<byte> binary)
        {
            binary.Add((byte)((value >> 8) & 0xFF));
            binary.Add((byte)(value & 0xFF));
        }

        private static void SerializeInt16MsbFirst(short value, List<byte> binary)
        {
            SerializeUInt16MsbFirst((ushort)value, binary);
        }

        private static int GetInt32(byte[] buffer, int offset)
        {
            if (buffer.Length < offset + 4)
                throw new ArgumentOutOfRangeException();

            int val = 
                  (buffer[offset] << 24)
                + (buffer[offset + 1] << 16)
                + (buffer[offset + 2] << 8)
                + (buffer[offset + 3]);
            return val;
        }

        private static uint GetUInt32(byte[] buffer, int offset)
        {
            return System.Convert.ToUInt32(GetInt32(buffer, offset));
        }

        private static short GetInt16(byte[] buffer, int offset)
        {
            if (buffer.Length < offset + 2)
                throw new ArgumentOutOfRangeException();

            short val = (short)(
                  (buffer[offset] << 8)
                + (buffer[offset + 1]));
            return val;
        }

        private static ushort GetUInt16(byte[] buffer, int offset)
        {
            return System.Convert.ToUInt16(GetInt16(buffer, offset));
        }
    }
}
