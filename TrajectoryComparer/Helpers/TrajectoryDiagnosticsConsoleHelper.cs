using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using TrajectoryComparer.Logging;

namespace TrajectoryComparer.Helpers
{
    internal static class TrajectoryDiagnosticsConsoleHelper
    {
        public static TrajectoryDiagnosticsRecord Load(string path, JsonSerializerOptions options)
        {
            var record = JsonSerializer.Deserialize<TrajectoryDiagnosticsRecord>(File.ReadAllText(path), options);
            if (record == null)
            {
                throw new InvalidOperationException($"Failed to deserialize diagnostics file: {path}");
            }

            return record;
        }

        public static void PrintComparisonReport(string diagnosticsDirectory, FileInfo leftFile, FileInfo rightFile, TrajectoryDiagnosticsRecord left, TrajectoryDiagnosticsRecord right)
        {
            PrintHeader(diagnosticsDirectory, leftFile, rightFile, left, right);
            PrintMetric("TotalPoints", left.TotalPoints, right.TotalPoints);
            PrintMetric("TotalChunks", left.TotalChunks, right.TotalChunks);
            PrintMetric("PointsPerChunk", left.PointsPerChunk, right.PointsPerChunk);
            PrintMetric("ChunkPayloadMaxBytes", left.ChunkPayloadMaxBytes, right.ChunkPayloadMaxBytes);
            PrintMetric("PointSizeBytes", left.PointSizeBytes, right.PointSizeBytes);
            PrintMetric("PayloadBytesTotal", left.PayloadBytesTotal, right.PayloadBytesTotal);
            PrintMetric("SentBytesTotal", left.SentBytesTotal, right.SentBytesTotal);
            PrintMetric("LaserOnPoints", left.Summary.LaserOnPoints, right.Summary.LaserOnPoints);
            PrintMetric("MinX", left.Summary.MinX, right.Summary.MinX);
            PrintMetric("MaxX", left.Summary.MaxX, right.Summary.MaxX);
            PrintMetric("WidthX", left.Summary.MaxX - left.Summary.MinX, right.Summary.MaxX - right.Summary.MinX);
            PrintMetric("MinY", left.Summary.MinY, right.Summary.MinY);
            PrintMetric("MaxY", left.Summary.MaxY, right.Summary.MaxY);
            PrintMetric("HeightY", left.Summary.MaxY - left.Summary.MinY, right.Summary.MaxY - right.Summary.MinY);
            PrintMetric("MinZ", left.Summary.MinZ, right.Summary.MinZ);
            PrintMetric("MaxZ", left.Summary.MaxZ, right.Summary.MaxZ);
            PrintMetric("DepthZ", left.Summary.MaxZ - left.Summary.MinZ, right.Summary.MaxZ - right.Summary.MinZ);

            Console.WriteLine();
            Console.WriteLine("First point:");
            PrintPointPair(left.Summary.FirstPoints.FirstOrDefault(), right.Summary.FirstPoints.FirstOrDefault());
            Console.WriteLine("Last point:");
            PrintPointPair(left.Summary.LastPoints.LastOrDefault(), right.Summary.LastPoints.LastOrDefault());

            Console.WriteLine();
            Console.WriteLine("Chunk analysis:");
            PrintChunkAnalysis(left, right);
        }

        private static void PrintHeader(string diagnosticsDirectory, FileInfo leftFile, FileInfo rightFile, TrajectoryDiagnosticsRecord left, TrajectoryDiagnosticsRecord right)
        {
            Console.WriteLine("Trajectory diagnostics comparison");
            Console.WriteLine($"Directory: {diagnosticsDirectory}");
            Console.WriteLine($"Left : {leftFile.Name} ({left.Timestamp:O})");
            Console.WriteLine($"Right: {rightFile.Name} ({right.Timestamp:O})");
            Console.WriteLine($"Source: {left.Source} -> {right.Source}");
            Console.WriteLine();
            Console.WriteLine($"{"Metric",-24} {"Left",14} {"Right",14} {"Delta",14}");
            Console.WriteLine(new string('-', 70));
        }

        private static void PrintMetric(string name, long left, long right)
        {
            Console.WriteLine($"{name,-24} {left,14} {right,14} {right - left,14}");
        }

        private static void PrintPointPair(TracePointSnapshot? left, TracePointSnapshot? right)
        {
            Console.WriteLine($"  Left : {FormatPoint(left)}");
            Console.WriteLine($"  Right: {FormatPoint(right)}");
        }

        private static string FormatPoint(TracePointSnapshot? point)
        {
            return point == null
                ? "<none>"
                : $"X={point.X}, Y={point.Y}, Z={point.Z}, LaserOn={point.LaserOn}";
        }

        private static void PrintChunkAnalysis(TrajectoryDiagnosticsRecord left, TrajectoryDiagnosticsRecord right)
        {
            int commonChunkCount = Math.Min(left.Chunks.Count, right.Chunks.Count);
            int differingChunks = 0;
            TrajectoryChunkRecord? firstDifferenceLeft = null;
            TrajectoryChunkRecord? firstDifferenceRight = null;

            for (int i = 0; i < commonChunkCount; i++)
            {
                var leftChunk = left.Chunks[i];
                var rightChunk = right.Chunks[i];
                if (!ChunkEquals(leftChunk, rightChunk))
                {
                    differingChunks++;
                    if (firstDifferenceLeft == null)
                    {
                        firstDifferenceLeft = leftChunk;
                        firstDifferenceRight = rightChunk;
                    }
                }
            }

            differingChunks += Math.Abs(left.Chunks.Count - right.Chunks.Count);
            Console.WriteLine($"  CommonChunkCount: {commonChunkCount}");
            Console.WriteLine($"  LeftChunkCount  : {left.Chunks.Count}");
            Console.WriteLine($"  RightChunkCount : {right.Chunks.Count}");
            Console.WriteLine($"  DifferingChunks : {differingChunks}");

            if (firstDifferenceLeft == null || firstDifferenceRight == null)
            {
                if (left.Chunks.Count != right.Chunks.Count)
                {
                    Console.WriteLine("  First difference: chunk count differs after common prefix.");
                }
                else
                {
                    Console.WriteLine("  First difference: no per-chunk differences detected.");
                }

                return;
            }

            Console.WriteLine("  First differing chunk:");
            Console.WriteLine($"    Left : #{firstDifferenceLeft.ChunkIndex}, StartIndex={firstDifferenceLeft.StartIndex}, Points={firstDifferenceLeft.Points}, Payload={firstDifferenceLeft.PayloadLength}, Frame={firstDifferenceLeft.FrameLength}, MinX={firstDifferenceLeft.Summary.MinX}, MaxX={firstDifferenceLeft.Summary.MaxX}, MinY={firstDifferenceLeft.Summary.MinY}, MaxY={firstDifferenceLeft.Summary.MaxY}, MinZ={firstDifferenceLeft.Summary.MinZ}, MaxZ={firstDifferenceLeft.Summary.MaxZ}");
            Console.WriteLine($"    Right: #{firstDifferenceRight.ChunkIndex}, StartIndex={firstDifferenceRight.StartIndex}, Points={firstDifferenceRight.Points}, Payload={firstDifferenceRight.PayloadLength}, Frame={firstDifferenceRight.FrameLength}, MinX={firstDifferenceRight.Summary.MinX}, MaxX={firstDifferenceRight.Summary.MaxX}, MinY={firstDifferenceRight.Summary.MinY}, MaxY={firstDifferenceRight.Summary.MaxY}, MinZ={firstDifferenceRight.Summary.MinZ}, MaxZ={firstDifferenceRight.Summary.MaxZ}");
        }

        private static bool ChunkEquals(TrajectoryChunkRecord left, TrajectoryChunkRecord right)
        {
            return left.StartIndex == right.StartIndex
                && left.Points == right.Points
                && left.PayloadLength == right.PayloadLength
                && left.FrameLength == right.FrameLength
                && left.AckSuccess == right.AckSuccess
                && left.AckReadedBytes == right.AckReadedBytes
                && left.Summary.MinX == right.Summary.MinX
                && left.Summary.MaxX == right.Summary.MaxX
                && left.Summary.MinY == right.Summary.MinY
                && left.Summary.MaxY == right.Summary.MaxY
                && left.Summary.MinZ == right.Summary.MinZ
                && left.Summary.MaxZ == right.Summary.MaxZ
                && left.Summary.LaserOnPoints == right.Summary.LaserOnPoints;
        }
    }
}
