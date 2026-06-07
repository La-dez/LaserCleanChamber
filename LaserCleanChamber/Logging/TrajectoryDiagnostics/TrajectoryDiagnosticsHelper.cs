using LaserCleanChamber.Model.Communication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using static LaserCleanChamber.Model.Communication.Protocol;

namespace LaserCleanChamber.Logging.TrajectoryDiagnostics
{
    public static class TrajectoryDiagnosticsHelper
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static string CreatePath(string sourceSuffix)
        {
            string diagnosticsDirectory = Path.Combine(AppContext.BaseDirectory, "TrajectoryDiagnostics");
            Directory.CreateDirectory(diagnosticsDirectory);
            return Path.Combine(diagnosticsDirectory, $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{sourceSuffix}-trajectory.json");
        }

        public static int GetChunkIndex(int startIndex, int pointsPerChunk)
        {
            return (startIndex / pointsPerChunk) + 1;
        }

        public static TrajectoryDiagnosticsRecord CreateRecord(string source, bool isEmulated, int maxPoints, int chunkPayloadMaxBytes, int pointsPerChunk, IReadOnlyList<TracePoint> tracePoints)
        {
            return new TrajectoryDiagnosticsRecord
            {
                Timestamp = DateTimeOffset.Now,
                Source = source,
                IsEmulated = isEmulated,
                MaxPoints = maxPoints,
                PointsPerChunk = pointsPerChunk,
                ChunkPayloadMaxBytes = chunkPayloadMaxBytes,
                PointSizeBytes = default(TracePoint).SizeInBytes,
                TotalPoints = tracePoints.Count,
                TotalChunks = (tracePoints.Count + pointsPerChunk - 1) / pointsPerChunk,
                PayloadBytesTotal = 0,
                SentBytesTotal = 0,
                Summary = BuildPointSummary(tracePoints),
                Chunks = new List<TrajectoryChunkRecord>()
            };
        }

        public static TrajectoryPointSummary BuildPointSummary(IReadOnlyList<TracePoint> points)
        {
            return new TrajectoryPointSummary
            {
                Count = points.Count,
                MinX = points.Min(p => p.X),
                MaxX = points.Max(p => p.X),
                MinY = points.Min(p => p.Y),
                MaxY = points.Max(p => p.Y),
                MinZ = points.Min(p => p.Z),
                MaxZ = points.Max(p => p.Z),
                LaserOnPoints = points.Count(p => p.LaserOn),
                FirstPoints = points.Take(5).Select(ToSnapshot).ToList(),
                LastPoints = points.Skip(Math.Max(0, points.Count - 5)).Select(ToSnapshot).ToList()
            };
        }

        public static void AppendChunk(TrajectoryDiagnosticsRecord? diagnosticsRecord, int chunkIndex, int startIndex, IReadOnlyList<TracePoint> tracePart, Frame request, SendTrajectoryResult result)
        {
            if (diagnosticsRecord == null)
                return;

            diagnosticsRecord.Chunks.Add(new TrajectoryChunkRecord
            {
                ChunkIndex = chunkIndex,
                StartIndex = startIndex,
                Points = tracePart.Count,
                PayloadLength = request.PayloadLength,
                FrameLength = request.FrameLength,
                AckSuccess = result.success,
                AckReadedBytes = result.readedBytes,
                Summary = BuildPointSummary(tracePart)
            });

            diagnosticsRecord.PayloadBytesTotal += request.PayloadLength;
            diagnosticsRecord.SentBytesTotal += request.FrameLength;
        }

        public static void Save(TrajectoryDiagnosticsRecord? diagnosticsRecord, string? diagnosticsPath)
        {
            if (diagnosticsRecord == null || diagnosticsPath == null)
                return;

            File.WriteAllText(diagnosticsPath, JsonSerializer.Serialize(diagnosticsRecord, JsonOptions));
        }

        private static TracePointSnapshot ToSnapshot(TracePoint point)
        {
            return new TracePointSnapshot
            {
                X = point.X,
                Y = point.Y,
                Z = point.Z,
                LaserOn = point.LaserOn
            };
        }
    }
}
