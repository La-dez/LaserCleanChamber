using System;
using System.Collections.Generic;

namespace TrajectoryComparer.Logging
{
    internal sealed class TrajectoryDiagnosticsRecord
    {
        public DateTimeOffset Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;
        public bool IsEmulated { get; set; }
        public int MaxPoints { get; set; }
        public int PointsPerChunk { get; set; }
        public int ChunkPayloadMaxBytes { get; set; }
        public int PointSizeBytes { get; set; }
        public int TotalPoints { get; set; }
        public int TotalChunks { get; set; }
        public int PayloadBytesTotal { get; set; }
        public int SentBytesTotal { get; set; }
        public TrajectoryPointSummary Summary { get; set; } = new();
        public List<TrajectoryChunkRecord> Chunks { get; set; } = new();
    }
}
