using g3;
using LaserCleanChamber.Model.Path;
using LaserCleanChamber.Model.Slicing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.TracingAlgorithms.Implementations
{
    public class ModifiedSnake : ITracingAlgorithm
    {
        public TracingAlgorithm AlgorithmType { get; } = TracingAlgorithm.SnakeModif;

        public List<PathSegment<g3.Vector2d>> GeneratePath2D(AxisAlignedBox2d bounds, double beamWidth, double overlap)
        {        //это просто алгоритм генерации линий, и все. Основная переработка в модифицированную змейку на 3D
            if (overlap >= 1) throw new Exception("Overlap must be in [0, 1)");

            var path = new List<PathSegment<g3.Vector2d>>();
            double Yshift = beamWidth * (1d - overlap);
            int N = (int)Math.Round(bounds.Height / Yshift) + 1; //rows count
            Yshift = bounds.Height / (N - 1);

            Vector2d P0 = bounds.Min;
            Vector2d currentPoint = P0;
            Vector2d center = bounds.Center;

            for (int n = 0; n < N; n++)
            {
                path.Add(new PathSegment<Vector2d>(
                    new Vector2d(P0.x, P0.y + Yshift * n),
                    new Vector2d(P0.x + bounds.Width, P0.y + Yshift * n),
                    true));
            }
            Console.WriteLine($"Snake 2v2 points^ {path.Count}");
            return path;
        }

        public List<PathSegment<Vector3d>> ProjectPathTo3D(
            GeometryModel? model,
            List<PathSegment<Vector2d>> path2d,
            double Margin,
            double traceStep,
            double zOffset,
            AxisAlignedBox2d ROI2d)
        {
            var path3d = new List<PathSegment<Vector3d>>();
            if (model == null || model.Mesh == null || path2d.Count == 0) return path3d;

            var spatialIndex = model.SpatialIndex;
            var bounds = model.Mesh.CachedBounds;

            double simplificationTolerance = 1e-4;

            List<List<PathSegment<Vector3d>>> path3dRipped = new List<List<PathSegment<Vector3d>>>(path2d.Count);
            for (int i = 0; i < path2d.Count; i++)
            {
                Vector2d p0 = path2d[i].p0;
                Vector2d p1 = path2d[i].p1;

                List<PathSegment<Vector3d>> traceResult = TracingHelpers.TraceAlongLine(spatialIndex, bounds, p0, p1, traceStep, Margin, zOffset, path2d[i].laserOn);
                if (path2d[i].laserOn && traceResult.Count > 0 && Margin > 0)
                    traceResult = TracingHelpers.ExtrapolateLineX(traceResult, Margin, ROI2d);
                List<PathSegment<Vector3d>> simplifiedSegment = TracingHelpers.SimplifyTrace(traceResult, simplificationTolerance);

                if (simplifiedSegment.Count > 0) path3dRipped.Add(simplifiedSegment);
            }

            path3d = PermutateAndReverse(path3dRipped);

            TracingHelpers.InsertInactiveSegments(path3d);
            var path3dOrto = TracingHelpers.OrtoApproxPath(path3d);
            return path3dOrto;
        }

        private static List<PathSegment<Vector3d>> PermutateAndReverse(List<List<PathSegment<Vector3d>>> segments)
        {
            var permuted = new List<PathSegment<Vector3d>>(segments.Count);
            var mod2 = segments.Count % 2;
            var median_m1 = (segments.Count / 2);
            var median = median_m1 + mod2;
            for (int i = 0; i < median_m1; i++)
            {
                permuted.AddRange(segments[i]);
                for (int j = 0; j < segments[i + median].Count; j++) //O(N^2), а что поделать
                    segments[i + median][j] = segments[i + median][j].GetReversed();
                segments[i + median].Reverse();
                permuted.AddRange(segments[i + median]);
            }
            if (mod2 == 1)
            {
                permuted.AddRange(segments[median_m1]);
            }
            return permuted;
        }
    }
}
