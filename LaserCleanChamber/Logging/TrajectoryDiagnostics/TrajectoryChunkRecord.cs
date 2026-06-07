namespace LaserCleanChamber.Logging.TrajectoryDiagnostics
{
    public sealed class TrajectoryChunkRecord
    {
        public int ChunkIndex { get; set; }
        public int StartIndex { get; set; }
        public int Points { get; set; }
        public int PayloadLength { get; set; }
        public int FrameLength { get; set; }
        public bool AckSuccess { get; set; }
        public int AckReadedBytes { get; set; }
        public TrajectoryPointSummary Summary { get; set; } = new TrajectoryPointSummary();
    }
}
