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
    public class StandardSnake: ITracingAlgorithm
    {
        public TracingAlgorithm AlgorithmType { get; } = TracingAlgorithm.Snake;

        public List<PathSegment<g3.Vector2d>> GeneratePath2D(AxisAlignedBox2d bounds, double beamWidth, double overlap)
        {
            if (/*overlap < 0 ||*/ overlap >= 1)
                throw new Exception("Overlap must be in [0, 1)");

            var path = new List<PathSegment<g3.Vector2d>>();
            double Yshift = beamWidth * (1d - overlap);
            int N = (int)Math.Round(bounds.Height / Yshift) + 1; //rows count
            Yshift = bounds.Height / (N - 1);

            Vector2d P0 = bounds.Min;
            Vector2d currentPoint = P0;
            Vector2d center = bounds.Center;

            for (int n = 0; n < N; n++)
            {
                if (currentPoint.x < center.x)
                    currentPoint.x += bounds.Width;
                else
                    currentPoint.x -= bounds.Width;

                path.Add(new PathSegment<Vector2d>(P0, currentPoint, true));
                P0 = currentPoint;

                if (n >= N - 1)
                    break;

                currentPoint.y += Yshift;
                //path.Add(new PathSegment<Vector2d>(P0, currentPoint, false));
                P0 = currentPoint;
            }
            Console.WriteLine($"Snake points^ {path.Count}");
            if (path.Count < 13)
            {

            }
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

            for (int i = 0; i < path2d.Count; i++)
            {
                Vector2d p0 = path2d[i].p0;
                Vector2d p1 = path2d[i].p1;

                List<PathSegment<Vector3d>> traceResult = TracingHelpers.TraceAlongLine(spatialIndex, bounds, p0, p1, traceStep, Margin, zOffset, path2d[i].laserOn);
                if (path2d[i].laserOn && traceResult.Count > 0 && Margin > 0)
                    traceResult = TracingHelpers.ExtrapolateLineX(traceResult, Margin, ROI2d);
                List<PathSegment<Vector3d>> simplifiedSegment = TracingHelpers.SimplifyTrace(traceResult, simplificationTolerance);

                path3d.AddRange(simplifiedSegment);
            }

            TracingHelpers.InsertInactiveSegments(path3d);
            var path3dOrto = TracingHelpers.OrtoApproxPath(path3d);
            return path3dOrto;
        }
    }
}
