using System.Collections.Generic;

namespace TrajectoryComparer.Logging
{
    internal sealed class TrajectoryPointSummary
    {
        public int Count { get; set; }
        public uint MinX { get; set; }
        public uint MaxX { get; set; }
        public uint MinY { get; set; }
        public uint MaxY { get; set; }
        public uint MinZ { get; set; }
        public uint MaxZ { get; set; }
        public int LaserOnPoints { get; set; }
        public List<TracePointSnapshot> FirstPoints { get; set; } = new();
        public List<TracePointSnapshot> LastPoints { get; set; } = new();
    }
}
