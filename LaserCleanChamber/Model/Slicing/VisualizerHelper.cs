using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using g3;
using HelixToolkit.Wpf;

namespace LaserCleanChamber.Model.Slicing
{
    public static class VisualizerHelper
    {

        public static LinesVisual3D CreatePathVisual(List<PathSegment<Vector3d>> path3d, Color color, double thickness = 2.0)
        {
            var linesVisual = new LinesVisual3D
            {
                Color = color,
                Thickness = thickness,
                DepthOffset = 0.0002
            };

            var points = new Point3DCollection();
            for (int i = 0; i < path3d.Count; i++)
            {
                var p0 = path3d[i].p0;
                var p1 = path3d[i].p1;

                points.Add(new Point3D(p0.x, p0.y, p0.z));
                points.Add(new Point3D(p1.x, p1.y, p1.z));
            }

            linesVisual.Points = points;
            return linesVisual;
        }

        /// <summary>
        /// Создает визуальный объект "Путь" для Helix Toolkit
        /// </summary>
        /// <param name="path3d">Список точек из вашего алгоритма</param>
        /// <param name="color">Цвет линии (например, Colors.Yellow)</param>
        /// <param name="thickness">Толщина линии</param>
        public static List<LinesVisual3D> CreatePathVisuals(List<PathSegment<Vector3d>> path3d, Color laserOnColor, Color laserOffColor, double thickness = 2.0)
        {
            List<LinesVisual3D> visuals = new List<LinesVisual3D>();

            if (path3d.Count < 1)
                return visuals;

            LinesVisual3D currentVisual = new LinesVisual3D();
            int startIndex = 0;
            int endIndex = 0;

            int index = 0;
            
            for(int i = 0; i < path3d.Count;)
            {
                List<PathSegment<Vector3d>> accum = new();
                for (int j = i; j < path3d.Count; j++)
                {
                    if (path3d[i].laserOn == path3d[j].laserOn)
                        accum.Add(path3d[j]);
                    else
                        break;
                }
                if (accum.Count == 0)
                    break;

                visuals.Add(CreatePathVisual(accum,
                    accum.Last().laserOn ? laserOnColor : laserOffColor,
                    thickness));

                i += accum.Count;
                
                accum.Clear();
            }
            
            return visuals;
        }

        /// <summary>
        /// Дополнительно: создает точки в местах перелома траектории (узлы)
        /// Это очень полезно для отладки SimplifyTrace
        /// </summary>
        //public static PointsVisual3D CreateNodesVisual(List<PathSegment<Vector3d>> path3d, Color color, double size = 5.0)
        //{
        //    var pointsVisual = new PointsVisual3D
        //    {
        //        Color = color,
        //        Size = size
        //    };

        //    foreach (var v in path3d)
        //    {
        //        pointsVisual.Points.Add(new Point3D(v.p0.x, v.p0.y, v.p0.z));
        //        pointsVisual.Points.Add(new Point3D(v.p1.x, v.p1.y, v.p1.z));
        //    }

        //    return pointsVisual;
        //}

        public static PointsVisual3D CreateNodesVisual(List<PathSegment<Vector3d>> path3d, Color color, double size = 5.0)
        {
            var points = new Point3DCollection(path3d.Count * 2);
            for (int i = 0; i < path3d.Count; i++)
            {
                var segment = path3d[i];
                points.Add(new Point3D(segment.p0.x, segment.p0.y, segment.p0.z));
                points.Add(new Point3D(segment.p1.x, segment.p1.y, segment.p1.z));
            }

            var pointsVisual = new PointsVisual3D
            {
                Color = color,
                Size = size,
                Points = points
            };

            return pointsVisual;
        }

        /// <summary>
        /// Конвертирует DMesh3 в формат, понятный Helix Toolkit для отрисовки.
        /// </summary>
        public static MeshGeometry3D ToHelixMesh(DMesh3 mesh)
        {
            var positions = new Point3DCollection();
            var triangleIndices = new System.Windows.Media.Int32Collection();
            var normals = new Vector3DCollection();

            // 1. Генерируем нормали вершин, если их нет
            // Это "магия", которая делает модель гладкой
            MeshNormals.QuickCompute(mesh);

            // 2. Заполняем вершины
            foreach (Vector3d vertex in mesh.Vertices())
            {
                positions.Add(new Point3D(vertex.x, vertex.y, vertex.z));
            }

            // 3. Заполняем нормали (теперь они есть в Mesh.Vertices())
            foreach (int vid in mesh.VertexIndices())
            {
                Vector3f n = mesh.GetVertexNormal(vid);
                normals.Add(new Vector3D(n.x, n.y, n.z));
            }

            // 4. Заполняем индексы
            foreach (Index3i triangle in mesh.Triangles())
            {
                triangleIndices.Add(triangle.a);
                triangleIndices.Add(triangle.b);
                triangleIndices.Add(triangle.c);
            }

            return new MeshGeometry3D
            {
                Positions = positions,
                TriangleIndices = triangleIndices,
                Normals = normals // Обязательно добавляем сюда!
            };
        }

        public static GeometryModel3D ToModel3D(MeshGeometry3D mesh, Color modelColor)
        {
            var diffuse = new DiffuseMaterial(new SolidColorBrush(modelColor));

            // Блик (белый блик делает деталь "живой")
            var specular = new SpecularMaterial(Brushes.White, 50); // 50 - жесткость блика

            var group = new MaterialGroup();
            group.Children.Add(diffuse);
            group.Children.Add(specular);

            var model3D = new GeometryModel3D
            {
                Geometry = mesh,
                Material = diffuse,
                BackMaterial = group // Чтобы деталь была видна изнутри/снизу
            };

            return model3D;
        }

        public static LinesVisual3D CreateSharpEdges(DMesh3 mesh, Color color, double angleThreshold = 20)
        {
            var lines = new LinesVisual3D { Color = color, Thickness = 1.5, DepthOffset = 0.0001 };
            var points = new Point3DCollection();

            foreach (int eid in mesh.EdgeIndices())
            {
                // Проверяем, является ли ребро граничным (у края дырки) 
                // или угол между нормалями соседних треугольников больше порога
                bool isSharp = false;

                if (mesh.IsBoundaryEdge(eid))
                {
                    isSharp = true;
                }
                else
                {
                    // Получаем индексы двух треугольников, разделяющих это ребро
                    Index2i edgeT = mesh.GetEdgeT(eid);
                    Vector3d n1 = mesh.GetTriNormal(edgeT.a);
                    Vector3d n2 = mesh.GetTriNormal(edgeT.b);

                    // Считаем угол между нормалями
                    double angle = Vector3d.AngleD(n1, n2);
                    if (angle > angleThreshold) isSharp = true;
                }

                if (isSharp)
                {
                    Index2i edgeV = mesh.GetEdgeV(eid);
                    Vector3d v0 = mesh.GetVertex(edgeV.a);
                    Vector3d v1 = mesh.GetVertex(edgeV.b);
                    points.Add(new Point3D(v0.x, v0.y, v0.z));
                    points.Add(new Point3D(v1.x, v1.y, v1.z));
                }
            }

            lines.Points = points;
            return lines;
        }

        public static LinesVisual3D CreateWireframe(DMesh3 mesh, Color color, double thickness = 0.5)
        {
            var lines = new LinesVisual3D { Color = color, Thickness = thickness, DepthOffset = 0.0001 };
            var points = new Point3DCollection();

            // Проходим по всем ребрам меша
            foreach (int eid in mesh.EdgeIndices())
            {
                Index2i edgeV = mesh.GetEdgeV(eid);
                Vector3d v0 = mesh.GetVertex(edgeV.a);
                Vector3d v1 = mesh.GetVertex(edgeV.b);

                points.Add(new Point3D(v0.x, v0.y, v0.z));
                points.Add(new Point3D(v1.x, v1.y, v1.z));
            }

            lines.Points = points;
            return lines;
        }
    }
}