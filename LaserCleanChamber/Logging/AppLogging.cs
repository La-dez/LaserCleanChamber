using LaserCleanChamber.Configuration;
using LaserCleanChamber.Model.Communication;
using LaserCleanChamber.Model.LaserComm;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static LaserCleanChamber.Model.Communication.Protocol;

namespace LaserCleanChamber.Logging
{
    public static class AppLogging
    {
        private const int TrajectoryPreviewCount = 10;

        private static readonly object SyncRoot = new object();
        private static readonly object DebounceSyncRoot = new object();
        private static readonly string SessionId = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        private static readonly Dictionary<string, DebouncedLogEntry> DebouncedEntries = new Dictionary<string, DebouncedLogEntry>();
        private static bool isInitialized;
        private static bool isEnabled = true;
        private static LogEventLevel minimumLevel = LogEventLevel.Information;

        public static ILogger App { get; private set; } = Logger.None;
        public static ILogger Controller { get; private set; } = Logger.None;
        public static ILogger Telemetry { get; private set; } = Logger.None;
        public static ILogger Laser { get; private set; } = Logger.None;

        public static string CurrentSessionId => SessionId;
        public static bool IsEnabled => isEnabled;

        public static void Initialize(LoggingSettings? settings)
        {
            if (isInitialized)
                return;

            lock (SyncRoot)
            {
                if (isInitialized)
                    return;

                var effectiveSettings = settings ?? new LoggingSettings();
                isEnabled = effectiveSettings.Enabled;

                if (!isEnabled)
                {
                    App = Logger.None;
                    Controller = Logger.None;
                    Telemetry = Logger.None;
                    Laser = Logger.None;
                    isInitialized = true;
                    return;
                }

                string logsRoot = Path.Combine(AppContext.BaseDirectory, "logs");
                minimumLevel = ParseLogLevel(effectiveSettings.MinimumLevel);

                App = effectiveSettings.EnabledAppLogger
                    ? CreateLogger(Path.Combine(logsRoot, "app", $"{SessionId}-app.log"), effectiveSettings.RetentionDaysAppLogs, minimumLevel)
                    : Logger.None;
                Controller = effectiveSettings.EnabledControllerLogger
                    ? CreateLogger(Path.Combine(logsRoot, "controller", $"{SessionId}-controller.log"), effectiveSettings.RetentionDaysControllerLogs, minimumLevel)
                    : Logger.None;
                Telemetry = effectiveSettings.EnabledTelemetryLogger
                    ? CreateLogger(Path.Combine(logsRoot, "telemetry", $"{SessionId}-telemetry.log"), effectiveSettings.RetentionDaysTelemetryLogs, minimumLevel)
                    : Logger.None;
                Laser = effectiveSettings.EnabledLaserLogger
                    ? CreateLogger(Path.Combine(logsRoot, "laser", $"{SessionId}-laser.log"), effectiveSettings.RetentionDaysLaserLogs, minimumLevel)
                    : Logger.None;

                isInitialized = true;
            }
        }

        public static string Prefix(string scope, string message)
        {
            return $"[{scope}] {message}";
        }

        public static void DebouncedInformation(string key, TimeSpan delay, ILogger logger, string messageTemplate, params object[] propertyValues)
        {
            if (!isEnabled)
                return;

            lock (DebounceSyncRoot)
            {
                if (DebouncedEntries.TryGetValue(key, out var existingEntry))
                {
                    existingEntry.PropertyValues = propertyValues;
                    existingEntry.MessageTemplate = messageTemplate;
                    existingEntry.Logger = logger;
                    existingEntry.Timer.Change(delay, Timeout.InfiniteTimeSpan);
                    return;
                }

                var entry = new DebouncedLogEntry
                {
                    Key = key,
                    Logger = logger,
                    MessageTemplate = messageTemplate,
                    PropertyValues = propertyValues
                };

                entry.Timer = new Timer(_ => FlushDebouncedEntry(entry.Key), null, delay, Timeout.InfiniteTimeSpan);
                DebouncedEntries[key] = entry;
            }
        }

        public static void CloseAndFlush()
        {
            lock (SyncRoot)
            {
                FlushAllDebouncedEntries();

                DisposeLogger(App);
                DisposeLogger(Controller);
                DisposeLogger(Telemetry);
                DisposeLogger(Laser);

                App = Logger.None;
                Controller = Logger.None;
                Telemetry = Logger.None;
                Laser = Logger.None;
                isEnabled = true;
                minimumLevel = LogEventLevel.Information;
                isInitialized = false;
            }
        }

        public static string DescribeFrame(Frame frame)
        {
            var messageType = Enum.IsDefined(typeof(MessageType), frame.MessageType)
                ? ((MessageType)frame.MessageType).ToString()
                : $"Unknown(0x{frame.MessageType:X2})";

            var builder = new StringBuilder();
            builder.Append($"Type={messageType}, PayloadLength={frame.PayloadLength}");

            switch ((MessageType)frame.MessageType)
            {
                case MessageType.MSG_LASER_CLEAN_SWITCH:
                case MessageType.MSG_LASER_CLEAN:
                    if (frame.PayloadLength > 0)
                        builder.Append($", Enabled={frame.Payload[0] == 1}");
                    break;
                case MessageType.MSG_SET_COOLDOWN_AND_REPEATS_CLEANING:
                    if (frame.PayloadLength >= 3)
                    {
                        ushort waitSeconds = (ushort)((frame.Payload[1] << 8) | frame.Payload[2]);
                        builder.Append($", Repeats={frame.Payload[0]}, WaitSeconds={waitSeconds}");
                    }
                    break;
                case MessageType.MSG_SET_COOLDOWN_LINES:
                    if (frame.PayloadLength >= 2)
                    {
                        ushort waitSeconds = (ushort)((frame.Payload[0] << 8) | frame.Payload[1]);
                        builder.Append($", WaitSeconds={waitSeconds}");
                    }
                    break;
                case MessageType.MSG_SET_LASER_POWER:
                    if (frame.PayloadLength > 0)
                        builder.Append($", Power={frame.Payload[0]}");
                    break;
                case MessageType.MSG_SET_GALVO_WIDTH:
                    if (frame.PayloadLength > 0)
                        builder.Append($", Width={frame.Payload[0]}");
                    break;
                case MessageType.MSG_SET_GALVO_SPEED:
                    if (frame.PayloadLength > 0)
                        builder.Append($", Speed={frame.Payload[0]}");
                    break;
                case MessageType.STMP_CALIBRATION:
                    if (frame.PayloadLength > 0)
                        builder.Append($", Axis={(MotorAxis)frame.Payload[0]}");
                    break;
                case MessageType.STMP_MOVE_COORD:
                    if (frame.PayloadLength >= 6)
                    {
                        uint steps = ReadUInt32BigEndian(frame.Payload, 2);
                        builder.Append($", Axis={(CoordAxis)frame.Payload[0]}, Direction={(Direction)frame.Payload[1]}, Steps={steps}");
                    }
                    break;
                case MessageType.STMP_MOVE_ABS_COORD:
                    if (frame.PayloadLength >= 5)
                    {
                        uint absCoord = ReadUInt32BigEndian(frame.Payload, 1);
                        builder.Append($", Axis={(CoordAxis)frame.Payload[0]}, Absolute={absCoord}");
                    }
                    break;
                case MessageType.STMP_SWITCH_OFF:
                    if (frame.PayloadLength > 0)
                        builder.Append($", DriverOn={frame.Payload[0] == 1}");
                    break;
                case MessageType.MSG_TELEMETRY:
                    builder.Append(DescribeTelemetryPayload(frame));
                    break;
                case MessageType.SEND_TRAECTORY:
                    builder.Append(DescribeTrajectoryPayload(frame.Payload));
                    break;
            }

            return builder.ToString();
        }

        public static string DescribeModbusFrame(byte[] frame, string direction)
        {
            if (frame == null || frame.Length < 2)
                return $"Direction={direction}, Invalid Modbus frame";

            byte slaveId = frame[0];
            byte function = frame[1];
            var builder = new StringBuilder();
            builder.Append($"Direction={direction}, SlaveId={slaveId}, Function=0x{function:X2}");

            if ((function & 0x80) != 0 && frame.Length >= 3)
            {
                builder.Append($", ExceptionCode={frame[2]}");
                return builder.ToString();
            }

            if (function == ModbusRtuHelper.FUNC_WRITE_SINGLE && frame.Length >= 6)
            {
                ushort address = ReadUInt16BigEndian(frame, 2);
                ushort value = ReadUInt16BigEndian(frame, 4);
                builder.Append($", Action=WriteSingleRegister, Register={DescribeRegister(address)}, Value={value}");
            }
            else if (function == ModbusRtuHelper.FUNC_READ_HOLDING && frame.Length >= 6)
            {
                if (direction == "Request")
                {
                    ushort address = ReadUInt16BigEndian(frame, 2);
                    ushort count = ReadUInt16BigEndian(frame, 4);
                    builder.Append($", Action=ReadHoldingRegisters, StartRegister={DescribeRegister(address)}, Count={count}");
                }
                else if (frame.Length >= 3)
                {
                    builder.Append($", Action=ReadHoldingRegistersResponse, ByteCount={frame[2]}");
                }
            }

            return builder.ToString();
        }

        public static string ToHex(byte[] bytes)
        {
            return bytes == null || bytes.Length == 0
                ? string.Empty
                : BitConverter.ToString(bytes).Replace("-", " ");
        }

        private static string DescribeTelemetryPayload(Frame frame)
        {
            try
            {
                var telemetry = Protocol.DecodeTelemetry(frame);
                return $", DoorClosed={telemetry.DoorClosed}, PlatePlaced={telemetry.PlatePlaced}, PistolPlaced={telemetry.PistolPlaced}, IsCleaning={telemetry.IsCleaning}, IsError={telemetry.IsError}, Temperature={telemetry.TemperatureInside_degC}";
            }
            catch
            {
                return ", TelemetryDecodeFailed=true";
            }
        }

        private static string DescribeTrajectoryPayload(byte[] payload)
        {
            if (payload.Length < 2)
                return ", StartIndex=unknown, Points=0";

            ushort startIndex = ReadUInt16BigEndian(payload, 0);
            int pointBytes = default(TracePoint).SizeInBytes;
            int pointsCount = (payload.Length - 2) / pointBytes;
            var points = new List<TracePoint>(pointsCount);

            for (int offset = 2; offset + pointBytes <= payload.Length; offset += pointBytes)
            {
                points.Add(new TracePoint
                {
                    X = ReadUInt32BigEndian(payload, offset),
                    Y = ReadUInt32BigEndian(payload, offset + 4),
                    Z = ReadUInt32BigEndian(payload, offset + 8),
                    LaserOn = payload[offset + 12] == 1
                });
            }

            string firstPoints = string.Join(" | ", points.Take(TrajectoryPreviewCount).Select(FormatTracePoint));
            string lastPoints = string.Join(" | ", points.Skip(Math.Max(0, points.Count - TrajectoryPreviewCount)).Select(FormatTracePoint));

            return $", StartIndex={startIndex}, Points={points.Count}, FirstPoints=[{firstPoints}], LastPoints=[{lastPoints}]";
        }

        private static string FormatTracePoint(TracePoint point)
        {
            return $"X={point.X},Y={point.Y},Z={point.Z},L={(point.LaserOn ? 1 : 0)}";
        }

        private static ushort ReadUInt16BigEndian(byte[] bytes, int offset)
        {
            return (ushort)((bytes[offset] << 8) | bytes[offset + 1]);
        }

        private static uint ReadUInt32BigEndian(byte[] bytes, int offset)
        {
            return (uint)((bytes[offset] << 24) |
                          (bytes[offset + 1] << 16) |
                          (bytes[offset + 2] << 8) |
                           bytes[offset + 3]);
        }

        private static string DescribeRegister(ushort address)
        {
            return address switch
            {
                LaserRegisters.SwingCenter => nameof(LaserRegisters.SwingCenter),
                LaserRegisters.SwingWidth => nameof(LaserRegisters.SwingWidth),
                LaserRegisters.SwingSpeed => nameof(LaserRegisters.SwingSpeed),
                LaserRegisters.SwingMode => nameof(LaserRegisters.SwingMode),
                LaserRegisters.WeldingCenter => nameof(LaserRegisters.WeldingCenter),
                LaserRegisters.SwingWidthCorrection => nameof(LaserRegisters.SwingWidthCorrection),
                LaserRegisters.WireRetractSpeed => nameof(LaserRegisters.WireRetractSpeed),
                LaserRegisters.WireFeedSpeed => nameof(LaserRegisters.WireFeedSpeed),
                LaserRegisters.WireRetractLength => nameof(LaserRegisters.WireRetractLength),
                LaserRegisters.WireFeedLength => nameof(LaserRegisters.WireFeedLength),
                LaserRegisters.LaserPowerOutput => nameof(LaserRegisters.LaserPowerOutput),
                LaserRegisters.LaserPowerCorrection => nameof(LaserRegisters.LaserPowerCorrection),
                LaserRegisters.PwmDutyCycle => nameof(LaserRegisters.PwmDutyCycle),
                LaserRegisters.WireRetractDelay => nameof(LaserRegisters.WireRetractDelay),
                LaserRegisters.PwmFrequency => nameof(LaserRegisters.PwmFrequency),
                LaserRegisters.PowerRiseTime => nameof(LaserRegisters.PowerRiseTime),
                LaserRegisters.LaserOnDelay => nameof(LaserRegisters.LaserOnDelay),
                LaserRegisters.GasOffDelay => nameof(LaserRegisters.GasOffDelay),
                LaserRegisters.LaserOffDelay => nameof(LaserRegisters.LaserOffDelay),
                LaserRegisters.InitialPower => nameof(LaserRegisters.InitialPower),
                LaserRegisters.MaxPower => nameof(LaserRegisters.MaxPower),
                LaserRegisters.AutoWireRetract => nameof(LaserRegisters.AutoWireRetract),
                LaserRegisters.SpotWeldingTime => nameof(LaserRegisters.SpotWeldingTime),
                LaserRegisters.SpotWeldingPause => nameof(LaserRegisters.SpotWeldingPause),
                LaserRegisters.WireCompensateLength => nameof(LaserRegisters.WireCompensateLength),
                LaserRegisters.PowerFallTime => nameof(LaserRegisters.PowerFallTime),
                LaserRegisters.MotorSpeedRatio => nameof(LaserRegisters.MotorSpeedRatio),
                LaserRegisters.MotorSpeedBias => nameof(LaserRegisters.MotorSpeedBias),
                LaserRegisters.ManualWireFeedSpeed => nameof(LaserRegisters.ManualWireFeedSpeed),
                LaserRegisters.ManualWireFeedLength => nameof(LaserRegisters.ManualWireFeedLength),
                LaserRegisters.MaxCurrent => nameof(LaserRegisters.MaxCurrent),
                LaserRegisters.MinCurrent => nameof(LaserRegisters.MinCurrent),
                LaserRegisters.PidProportional => nameof(LaserRegisters.PidProportional),
                LaserRegisters.PidIntegral => nameof(LaserRegisters.PidIntegral),
                LaserRegisters.PidDerivative => nameof(LaserRegisters.PidDerivative),
                LaserRegisters.PidFeedForward => nameof(LaserRegisters.PidFeedForward),
                LaserRegisters.MeltWeldingCorrection => nameof(LaserRegisters.MeltWeldingCorrection),
                LaserRegisters.CenterSpeed => nameof(LaserRegisters.CenterSpeed),
                LaserRegisters.Backlash => nameof(LaserRegisters.Backlash),
                LaserRegisters.WireCutPower => nameof(LaserRegisters.WireCutPower),
                LaserRegisters.ActivationState => nameof(LaserRegisters.ActivationState),
                LaserRegisters.SpotWireFeedTime => nameof(LaserRegisters.SpotWireFeedTime),
                LaserRegisters.SpotWireStopTime => nameof(LaserRegisters.SpotWireStopTime),
                LaserRegisters.WeldingModeSelect => nameof(LaserRegisters.WeldingModeSelect),
                LaserRegisters.CommandWord => nameof(LaserRegisters.CommandWord),
                LaserRegisters.InputStatus => nameof(LaserRegisters.InputStatus),
                LaserRegisters.OutputStatus => nameof(LaserRegisters.OutputStatus),
                LaserRegisters.SystemControl => nameof(LaserRegisters.SystemControl),
                _ => $"0x{address:X4}"
            };
        }

        private static ILogger CreateLogger(string path, int retentionDays, LogEventLevel level)
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return new LoggerConfiguration()
                .MinimumLevel.Is(level)
                .WriteTo.File(
                    path,
                    restrictedToMinimumLevel: level,
                    retainedFileCountLimit: Math.Max(1, retentionDays),
                    rollingInterval: RollingInterval.Infinite,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        private static LogEventLevel ParseLogLevel(string? level)
        {
            if (Enum.TryParse<LogEventLevel>(level, true, out var parsedLevel))
            {
                return parsedLevel;
            }

            return LogEventLevel.Information;
        }

        private static void DisposeLogger(ILogger logger)
        {
            if (logger is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private static void FlushDebouncedEntry(string key)
        {
            DebouncedLogEntry? entry;

            lock (DebounceSyncRoot)
            {
                if (!DebouncedEntries.TryGetValue(key, out entry))
                    return;

                DebouncedEntries.Remove(key);
            }

            try
            {
                entry.Logger.Information(entry.MessageTemplate, entry.PropertyValues);
            }
            finally
            {
                entry.Timer.Dispose();
            }
        }

        private static void FlushAllDebouncedEntries()
        {
            List<string> keys;

            lock (DebounceSyncRoot)
            {
                keys = DebouncedEntries.Keys.ToList();
            }

            foreach (var key in keys)
            {
                FlushDebouncedEntry(key);
            }
        }

        private sealed class DebouncedLogEntry
        {
            public string Key { get; set; } = string.Empty;
            public ILogger Logger { get; set; } = Serilog.Core.Logger.None;
            public string MessageTemplate { get; set; } = string.Empty;
            public object[] PropertyValues { get; set; } = Array.Empty<object>();
            public Timer Timer { get; set; } = null!;
        }
    }
}
