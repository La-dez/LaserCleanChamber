using HelixToolkit.Wpf;
using LaserCleanChamber.Model.Slicing;
using LaserCleanChamber.ViewModel;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace LaserCleanChamber.View.Controls
{
    public partial class TracingEditor : UserControl
    {
        private Color modelColor = Colors.LightGray;
        private Color laserOnColor = Colors.Red;
        private Color laserOffColor = Colors.Blue;

        // Визуальные элементы для рамки
        private LinesVisual3D regionLines = new LinesVisual3D { Color = Colors.LimeGreen, Thickness = 2 };
        private PointsVisual3D regionCorners = new PointsVisual3D { Color = Colors.Yellow, Size = 10 };

        // Переменные для перетаскивания
        private bool isDragging = false;
        private int draggedCornerIndex = -1; // 0: TopLeft, 1: TopRight, 2: BottomRight, 3: BottomLeft
        private TracingViewModel? currentViewModel;

        public TracingEditor()
        {
            InitializeComponent();
            //MainViewport.Children.Add(regionLines);
            MainViewport.Children.Add(regionCorners);
            DataContextChanged += TracingEditor_DataContextChanged;
        }

        private void TracingEditor_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is TracingViewModel oldTracing)
                oldTracing.PropertyChanged -= Tracing_PropertyChanged;

            currentViewModel = e.NewValue as TracingViewModel;

            if (currentViewModel != null)
            {
                currentViewModel.PropertyChanged += Tracing_PropertyChanged;
                double ratio = MainViewport.ActualWidth / MainViewport.ActualHeight;
                if (ratio == 0 || double.IsNaN(ratio) || double.IsInfinity(ratio))
                    ratio = 1;
                
                camera.Width = currentViewModel.Model?.Mesh.GetBounds().Width * ratio * 1.3 ?? 100;
            }
            UpdateVisual();
        }

        private void Tracing_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TracingViewModel.Trace) ||
                e.PropertyName == nameof(TracingViewModel.Model))
            {
                UpdateVisual();
            }

            // Если изменился регион (например, при загрузке модели или из-за перетаскивания), обновляем только рамку
            if (e.PropertyName == nameof(TracingViewModel.TracingRegion))
            {
                UpdateRegionVisual();
            }
        }

        private void UpdateVisual()
        {
            if (currentViewModel == null)
            {
                ModelVisualizer.Content = null;
                return;
            }

            try
            {
                // Удаляем старые пути, но оставляем regionLines и regionCorners
                var paths = MainViewport.Children.OfType<LinesVisual3D>().Where(l => l != regionLines).ToList();
                paths.ForEach(p => MainViewport.Children.Remove(p));
                var pointsVisuals = MainViewport.Children.OfType<PointsVisual3D>().Where(l => l != regionCorners).ToList();
                pointsVisuals.ForEach(p => MainViewport.Children.Remove(p));

                if (currentViewModel.Model?.Mesh != null)
                {
                    ModelVisualizer.Content = VisualizerHelper.ToModel3D(VisualizerHelper.ToHelixMesh(currentViewModel.Model.Mesh), modelColor);
                    var sharpEdges = VisualizerHelper.CreateSharpEdges(currentViewModel.Model.Mesh, Colors.Black, 15);
                    MainViewport.Children.Add(sharpEdges);
                }

                if (currentViewModel.Trace != null)
                {
                    var pathVisuals = VisualizerHelper.CreatePathVisuals(currentViewModel.Trace, laserOnColor, laserOffColor, 2.0);

                    foreach(var visual in pathVisuals)
                        MainViewport.Children.Add(visual);

                    var nodeVisuals = VisualizerHelper.CreateNodesVisual(currentViewModel.Trace, Colors.SteelBlue, 5);
                    MainViewport.Children.Add(nodeVisuals);
                }

                UpdateRegionVisual();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        // Обрисовка рамки на основе ViewModel
        private void UpdateRegionVisual()
        {
            if (currentViewModel == null) return;

            var rect = currentViewModel.TracingRegion;
            double z = 5.0; // Высота над моделью, чтобы рамку было видно

            // Точки: TopLeft, TopRight, BottomRight, BottomLeft
            var p0 = new Point3D(rect.X, rect.Y + rect.Height, z);
            var p1 = new Point3D(rect.X + rect.Width, rect.Y + rect.Height, z);
            var p2 = new Point3D(rect.X + rect.Width, rect.Y, z);
            var p3 = new Point3D(rect.X, rect.Y, z);

            regionCorners.Points.Clear();
            regionCorners.Points.Add(p0);
            regionCorners.Points.Add(p1);
            regionCorners.Points.Add(p2);
            regionCorners.Points.Add(p3);

            regionLines.Points.Clear();
            regionLines.Points.Add(p0); regionLines.Points.Add(p1);
            regionLines.Points.Add(p1); regionLines.Points.Add(p2);
            regionLines.Points.Add(p2); regionLines.Points.Add(p3);
            regionLines.Points.Add(p3); regionLines.Points.Add(p0);
        }

        #region Интерактивное перетаскивание рамки (Mouse Events)

        private void MainViewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (currentViewModel == null || e.LeftButton != MouseButtonState.Pressed) return;

            Point mousePos2D = e.GetPosition(MainViewport);

            // Ищем, по какому углу кликнули (в радиусе 15 пикселей на экране)
            for (int i = 0; i < regionCorners.Points.Count; i++)
            {
                var point3D = regionCorners.Points[i];
                var point2D = MainViewport.Viewport.Point3DtoPoint2D(point3D);

                if ((point2D - mousePos2D).Length < 15) // Порог в пикселях
                {
                    isDragging = true;
                    draggedCornerIndex = i;
                    MainViewport.CaptureMouse();
                    e.Handled = true;
                    return;
                }
            }
        }

        private void MainViewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || currentViewModel == null) return;

            Point mousePos2D = e.GetPosition(MainViewport);

            // Получаем луч из камеры в точку мыши
            var ray = MainViewport.Viewport.Point2DtoRay3D(mousePos2D);
            if (ray == null) return;

            // Находим пересечение луча с плоскостью Z = 1 (где лежит наша рамка)
            var plane = new Plane3D(new Point3D(0, 0, 1), new Vector3D(0, 0, 1));
            Point3D p1 = ray.Origin;
            Point3D p2 = ray.Origin + ray.Direction * 1000;
            var intersection = plane.LineIntersection(new Vector3D(p1.X, p1.Y, p1.Z), new Vector3D(p2.X, p2.Y, p2.Z));

            if (intersection.HasValue)
            {
                Rect currentRect = currentViewModel.TracingRegion;
                double newX = currentRect.X;
                double newY = currentRect.Y;
                double newRight = currentRect.X + currentRect.Width;
                double newTop = currentRect.Y + currentRect.Height;

                // В зависимости от того, какой угол тянем - меняем координаты
                switch (draggedCornerIndex)
                {
                    case 0: // TopLeft
                        newX = intersection.Value.X;
                        newTop = intersection.Value.Y;
                        break;
                    case 1: // TopRight
                        newRight = intersection.Value.X;
                        newTop = intersection.Value.Y;
                        break;
                    case 2: // BottomRight
                        newRight = intersection.Value.X;
                        newY = intersection.Value.Y;
                        break;
                    case 3: // BottomLeft
                        newX = intersection.Value.X;
                        newY = intersection.Value.Y;
                        break;
                }

                // Предотвращаем "выворачивание" прямоугольника наизнанку
                if (newRight > newX + 1 && newTop > newY + 1)
                {
                    currentViewModel.TracingRegion = new Rect(newX, newY, newRight - newX, newTop - newY);
                }
            }
        }

        private void MainViewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                MainViewport.ReleaseMouseCapture();
            }
        }

        #endregion

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}