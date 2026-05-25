using LaserCleanChamber.Configuration;
using LaserCleanChamber.Model.Slicing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFMediaKit.DirectShow.Controls;

namespace LaserCleanChamber.Model
{
    public enum TracingAlgorithm : int { Snake, SnakeModif }
    public enum PlateSides : int { Top, Bottom }
    
    public class CleaningSession
    {
        public TracingAlgorithm TracingAlgorithm { get; set; } = TracingAlgorithm.Snake;
        public PlateSides PlateSide { get; set; } = PlateSides.Top;


        private GeometryModel model3D;
        private GeometryModel model3DFlipped;

        public GeometryModel? GetModel3D()
        {
            switch (PlateSide)
            {
                case PlateSides.Top:
                    return this.model3D;
                case PlateSides.Bottom:
                    return this.model3DFlipped;
            }
            return null;
        }

        public Rect ROI { get; set; }
        public double Overlap { get; set; }
        public double TraceStep { get; set; }
        public double ApproxError { get; set; }
        public double Margin { get; set; }
        public LaserPreset? SelectedPreset { get; set; }
        //public List<g3.Vector3d>? Trajectory { get; private set; }
        public List<PathSegment<g3.Vector3d>>? Trajectory { get; private set; }

        public CleaningSession(AppSettings settings)
        {
            this.model3D = GeometryModel.LoadFromFile(settings.Tracing.ModelFileName);
            this.model3DFlipped = model3D.CreateFlippedVersion(false);

            if (Enum.TryParse(typeof(TracingAlgorithm), settings.Tracing.Algorythm, true, out object? tracingAlgo))
            {
                TracingAlgorithm = (TracingAlgorithm)tracingAlgo;
            }
            Overlap = settings.Tracing.Overlap;
            TraceStep = settings.Tracing.TraceStep;
            ApproxError = settings.Tracing.ApproxError;
            Margin = settings.Tracing.Margin;

            var bounds = model3D.Mesh.GetBounds();
            //ROI = new Rect(bounds.Min.x, bounds.Min.y, bounds.Max.x - bounds.Min.x, bounds.Max.y - bounds.Min.y);
            ROI = new Rect(-50, -60, 100, 120);
        }

        public void CalculateTrajectory_v1()
        {
            GeometryModel? model = GetModel3D();

            if (model == null || SelectedPreset == null)
                throw new InvalidOperationException("Нет модели или пресета");

            var bounds2d = new g3.AxisAlignedBox2d(ROI.X, ROI.Y, ROI.X + ROI.Width, ROI.Y + ROI.Height);

            List<PathSegment<g3.Vector2d>> path2d = new List<PathSegment<g3.Vector2d>>();
            switch (TracingAlgorithm)
            {
                case TracingAlgorithm.Snake:
                    path2d = PathGenerator.GenerateSnakePath2D(bounds2d, SelectedPreset.ScanWidth, Overlap / 100);
                    break;
                case TracingAlgorithm.SnakeModif:
                    path2d = PathGenerator.GenerateSnakeModifPath2D(bounds2d, SelectedPreset.ScanWidth, Overlap / 100);
                    break;
                default:
                    throw new NotImplementedException();
            }

            var path = PathGenerator.ProjectPathTo3D(model.Mesh, path2d, Margin, 1, 0);

            Trajectory = path;
        }

        public void CalculateTrajectory()
        {
            GeometryModel? model = GetModel3D();

            if (model == null || SelectedPreset == null)
                throw new InvalidOperationException("Нет модели или пресета");

            var bounds2d = new g3.AxisAlignedBox2d(ROI.X, ROI.Y, ROI.X + ROI.Width, ROI.Y + ROI.Height);

            List<PathSegment<g3.Vector2d>> path2d = new List<PathSegment<g3.Vector2d>>();
            switch (TracingAlgorithm)
            {
                case TracingAlgorithm.Snake:
                    path2d = PathGenerator.GenerateSnakePath2D(bounds2d, SelectedPreset.ScanWidth, Overlap / 100);
                    break;
                case TracingAlgorithm.SnakeModif:
                    path2d = PathGenerator.GenerateSnakeModifPath2D(bounds2d, SelectedPreset.ScanWidth, Overlap / 100);
                    break;
                default:
                    throw new NotImplementedException();
            }

            var path = PathGenerator.ProjectPathTo3D_v2(model.Mesh, path2d, Margin, 1, 0, bounds2d);

            Trajectory = path;
        }

        public string AutoSelectCamera(string videoDeviceName)
        {
            var availableDevices = MultimediaUtil.VideoInputDevices;

            if (availableDevices == null || availableDevices.Length == 0)
            {
                return "";
            }

            var myDevice = availableDevices
                .FirstOrDefault(d => d.Name.IndexOf(videoDeviceName, StringComparison.OrdinalIgnoreCase) >= 0);

            if (myDevice == null)
            {
                myDevice = availableDevices.FirstOrDefault();
            }

            if (myDevice != null)
            {
                return myDevice.Name;
            }
            return "";
        }

    }
}
