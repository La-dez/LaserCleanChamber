using g3;
using LaserCleanChamber.Model.Path;
using LaserCleanChamber.Model.Slicing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.TracingAlgorithms
{
    public enum TracingAlgorithm : int
    {
        Snake, 
        SnakeModif
    }
    public interface ITracingAlgorithm
    {
        TracingAlgorithm AlgorithmType { get; }

        List<PathSegment<g3.Vector2d>> GeneratePath2D(AxisAlignedBox2d bounds, double beamWidth, double overlap);

        List<PathSegment<Vector3d>> ProjectPathTo3D(GeometryModel? model,
           List<PathSegment<Vector2d>> path2d,
           double Margin,
           double traceStep,
           double zOffset,
           AxisAlignedBox2d ROI2d); //to refactor

    }
}
