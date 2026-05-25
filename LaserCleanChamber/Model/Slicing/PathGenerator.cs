using ControlzEx.Standard;
using g3;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace LaserCleanChamber.Model.Slicing
{
    public struct PathSegment<T> where T : new()
    {
        public T p0 = new T();
        public T p1 = new T();
        public bool laserOn;

        public PathSegment() { }

        public PathSegment(T p0, T p1, bool laserOn)
        {
            this.p0 = p0;
            this.p1 = p1;
            this.laserOn = laserOn;
        }

        public override string ToString()
        {
            return $"P0={p0.ToString()} P1={p1.ToString()}";
        }
    }

    public static class PathGenerator
    {
        /// <summary>
        /// Создает 2D-траекторию типа "змейка" внутри заданных границ.
        /// </summary>
        /// <param name="bounds">Прямоугольник, который нужно покрыть.</param>
        /// <param name="beamSettings">Настройки лазерного пучка.</param>
        /// <returns>Список 2D-точек траектории.</returns>
        public static List<PathSegment<g3.Vector2d>> GenerateSnakeModifPath2D(AxisAlignedBox2d bounds, double beamWidth, double overlap)
        {
            if (/*overlap < 0 ||*/ overlap >= 1)
                throw new Exception("Overlap must be in [0, 1)");

            var path = new List<PathSegment<g3.Vector2d>>();
            double Yshift = beamWidth * (1d - overlap);
            int N = (int)Math.Round(bounds.Height / Yshift) + 1;
            Yshift = bounds.Height / (N - 1);

            List<int> rowNumbers = new List<int>(N);
            int i_center = (int)Math.Ceiling(N / 2.0);
            for (int i = 0; i < N; i++)
            {
                rowNumbers.Add(i / 2 + i_center * (i % 2));
            }

            Vector2d P0 = bounds.Min;
            Vector2d currentPoint = P0;
            Vector2d center = bounds.Center;

            for (int n = 0; n < rowNumbers.Count; n++)
            {
                if (currentPoint.x < center.x)
                    currentPoint.x += bounds.Width;
                else
                    currentPoint.x -= bounds.Width;

                currentPoint.y = bounds.Min.y + rowNumbers[n] * Yshift;

                path.Add(new PathSegment<Vector2d>(P0, currentPoint, true));
                P0 = currentPoint;

                if (n >= rowNumbers.Count - 1)
                    break;

                currentPoint.y = bounds.Min.y + rowNumbers[n + 1] * Yshift;

                //path.Add(new PathSegment<Vector2d>(P0, currentPoint, false));
                P0 = currentPoint;
            }
            return path;
        }

        public static List<PathSegment<g3.Vector2d>> GenerateSnakePath2D(AxisAlignedBox2d bounds, double beamWidth, double overlap)
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

        public static List<PathSegment<Vector3d>> ProjectPathTo3D(DMesh3 mesh, List<PathSegment<Vector2d>> path2d,
            double Margin, double traceStep, double zOffset)
        {
            var path3d = new List<PathSegment<Vector3d>>();
            if (mesh == null || path2d.Count == 0) return path3d;

            var spatialIndex = new DMeshAABBTree3(mesh, true);
            var bounds = mesh.CachedBounds;
            
            double simplificationTolerance = 1e-4;

            for (int i = 0; i < path2d.Count; i++)
            {
                Vector2d p0 = path2d[i].p0;
                Vector2d p1 = path2d[i].p1;

                List<PathSegment<Vector3d>> traceResult = TraceAlongLine(spatialIndex, bounds, p0, p1, traceStep, Margin, zOffset, path2d[i].laserOn);
                if (path2d[i].laserOn && traceResult.Count > 0 && Margin > 0)
                    traceResult = ExtrapolateLine(traceResult, bounds.Center, Margin);
                List<PathSegment<Vector3d>> simplifiedSegment = SimplifyTrace(traceResult, simplificationTolerance);

                path3d.AddRange(simplifiedSegment);
            }

            for (int i = 0; i < path3d.Count - 1; i++)
            {
                var first = path3d[i];
                var second = path3d[i + 1];

                if (first.p1 != second.p0)
                {
                    path3d.Insert(i + 1, new PathSegment<Vector3d>(first.p1, second.p0, false));
                    i++;
                }
            }

            var path3dOrto = new List<PathSegment<Vector3d>>();
            for (int i = 0; i < path3d.Count; i++)
            {
                var ortoSeg = OrtoApproxSegment(path3d[i], 3);
                path3dOrto.AddRange(ortoSeg);
            }

            return path3dOrto; 
            return path3d;
        }

        public static List<PathSegment<Vector3d>> ProjectPathTo3D_v2(DMesh3 mesh, List<PathSegment<Vector2d>> path2d,
           double Margin, double traceStep, double zOffset, AxisAlignedBox2d ROI2d)
        {
            var path3d = new List<PathSegment<Vector3d>>();
            if (mesh == null || path2d.Count == 0) return path3d;

            var spatialIndex = new DMeshAABBTree3(mesh, true);
            var bounds = mesh.CachedBounds;

            double simplificationTolerance = 1e-4;

            for (int i = 0; i < path2d.Count; i++)
            {
                Vector2d p0 = path2d[i].p0;
                Vector2d p1 = path2d[i].p1;

                List<PathSegment<Vector3d>> traceResult = TraceAlongLine(spatialIndex, bounds, p0, p1, traceStep, Margin, zOffset, path2d[i].laserOn);
                if (path2d[i].laserOn && traceResult.Count > 0 && Margin > 0)
                    traceResult = ExtrapolateLine_v2(traceResult, Margin, ROI2d);
                List<PathSegment<Vector3d>> simplifiedSegment = SimplifyTrace(traceResult, simplificationTolerance);

                path3d.AddRange(simplifiedSegment);
            }

            for (int i = 0; i < path3d.Count - 1; i++)
            {
                var first = path3d[i];
                var second = path3d[i + 1];

                if (first.p1 != second.p0)
                {
                    path3d.Insert(i + 1, new PathSegment<Vector3d>(first.p1, second.p0, false));
                    i++;
                }
            }

            var path3dOrto = new List<PathSegment<Vector3d>>();
            for (int i = 0; i < path3d.Count; i++)
            {
                var ortoSeg = OrtoApproxSegment(path3d[i], 3);
                path3dOrto.AddRange(ortoSeg);
            }

            return path3dOrto;
        }


        private static double Hypot(double x, double y, double z = 0)
        {
            return Math.Sqrt(x * x + y * y + z * z);
        }

        private static List<PathSegment<Vector3d>> ExtrapolateLine(List<PathSegment<Vector3d>> traceResult, Vector3d center, double margin)
        {
            if (traceResult.Count == 0 || margin <= 0)
                return traceResult;

            var extrapolated = new List<PathSegment<Vector3d>>(traceResult.Count + 2);

            PathSegment<Vector3d> first = traceResult.First();
            Vector3d firstDirection = (first.p1 - first.p0).Normalized;
            if (firstDirection.LengthSquared > 0)
            {
                Vector3d outwardFromStart = (first.p0 - center).Normalized;
                Vector3d startDirection = firstDirection.Dot(outwardFromStart) >= (-firstDirection).Dot(outwardFromStart)
                    ? firstDirection
                    : -firstDirection;
                Vector3d newStart = first.p0 + startDirection * margin;
                extrapolated.Add(new PathSegment<Vector3d>(newStart, first.p0, true));
            }

            extrapolated.AddRange(traceResult);

            PathSegment<Vector3d> last = traceResult.Last();
            Vector3d lastDirection = (last.p1 - last.p0).Normalized;
            if (lastDirection.LengthSquared > 0)
            {
                Vector3d outwardFromEnd = (last.p1 - center).Normalized;
                Vector3d endDirection = lastDirection.Dot(outwardFromEnd) >= (-lastDirection).Dot(outwardFromEnd)
                    ? lastDirection
                    : -lastDirection;
                Vector3d newEnd = last.p1 + endDirection * margin;
                extrapolated.Add(new PathSegment<Vector3d>(last.p1, newEnd, true));
            }

            return extrapolated;
        }

        private static List<PathSegment<Vector3d>> ExtrapolateLine_v2(List<PathSegment<Vector3d>> traceResult, double xMargins, AxisAlignedBox2d bounds2D)
        {

            var lastSegment = traceResult.Last();
            var firstSegment = traceResult.First();
            bool invertionCoef = firstSegment.p1.x > firstSegment.p0.x ;
            double newStartX = invertionCoef ? 
                            Math.Max(firstSegment.p0.x - xMargins, bounds2D.Min.x) : 
                            Math.Min(firstSegment.p0.x + xMargins, bounds2D.Max.x);
            double newEndX = invertionCoef ?
                            Math.Min(lastSegment.p1.x + xMargins, bounds2D.Max.x) :
                            Math.Max(lastSegment.p1.x - xMargins, bounds2D.Min.x);
            traceResult.Insert(0,
                new PathSegment<Vector3d>(
                    new Vector3d(newStartX, firstSegment.p0.y, firstSegment.p0.z),
                    firstSegment.p0,
                    true)
                );
            traceResult.Add(new PathSegment<Vector3d>(
                lastSegment.p1,
                new Vector3d(newEndX, lastSegment.p1.y, lastSegment.p1.z),
                true)
                );

            return traceResult;
        }

        private static double Shortest(double a, double b, double c)
        {
            double ab = a * b;
            double L_ab = Hypot(a, b);
            double bc = b * c;
            double L_bc = Hypot(b, c);
            double ca = c * a;
            double L_ca = Hypot(c, a);

            double x = ab;
            if (L_ab != 0)
                x /= L_ab;
            else
                x = 0;

            double y = bc;
            if (L_bc != 0)
                y /= L_bc;
            else
                y = 0;

            double z = ca;
            if (L_ca != 0)
                z /= L_ca;
            else
                z = 0;

            return Hypot(x, y, z);
        }

        public static int CalculateSteps(double dx, double dy, double dz, double h)
        {
            double lTotal = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            // Если перемещение почти нулевое, разбиение не нужно
            if (lTotal < 0.0001) return 1;

            // Отклонение в первой точке излома (после X)
            double dev1 = (Math.Abs(dx) * Math.Sqrt(dy * dy + dz * dz)) / lTotal;

            // Отклонение во второй точке излома (после Y)
            double dev2 = (Math.Abs(dz) * Math.Sqrt(dx * dx + dy * dy)) / lTotal;

            double maxDev = Math.Max(dev1, dev2);

            // Если текущее отклонение уже меньше допустимого, возвращаем 1 сегмент
            if (maxDev <= h) return 1;

            // Рассчитываем необходимое количество сегментов N
            return (int)Math.Ceiling(maxDev / h);
        }

        private static List<PathSegment<Vector3d>> OrtoApproxSegment(PathSegment<Vector3d> segment, double h_min)
        {
            List<PathSegment<Vector3d>> path = new List<PathSegment<Vector3d>>();
            Vector3d dist = segment.p1 - segment.p0;
            double h = Shortest(dist.x, dist.y, dist.z);

            int n = CalculateSteps(Math.Abs(dist.x), Math.Abs(dist.y), Math.Abs(dist.z), h_min);

            if (n <= 1)
            {
                path.Add(segment);
            }
            else
            {
                Vector3d dd = dist / n;
                Vector3d p0 = segment.p0;
                Vector3d p1 = p0;
                for (int i = 0; i < n; i++)
                {
                    if (i == n - 1)
                        p1 = segment.p1;
                    else
                        p1 = p0 + dd;
                    path.Add(new PathSegment<Vector3d>(p0, p1, segment.laserOn));
                    p0 = p1;
                }
            }
            return path;
        }

        private static List<PathSegment<Vector3d>> TraceAlongLine(DMeshAABBTree3 spatialIndex, AxisAlignedBox3d bounds,
            Vector2d p0, Vector2d p1, double stepLength,
            double xMargins, double zOffset, bool laserEnable)
        {
            var result = new List<PathSegment<Vector3d>>();

            double totalDistance = p0.Distance(p1);
            Vector2d direction = (p1 - p0).Normalized;

            // Количество шагов (округляем вверх, чтобы не потерять конец)
            int steps = (int)Math.Ceiling(totalDistance / stepLength);

            // Стартовая высота луча (чуть выше максимума детали)
            double rayOriginZ = bounds.Max.z + 10.0;

            Vector3d lastHit = new();
            int validHits = 0;
            for (int i = 0; i <= steps; i++)
            {
                // Вычисляем текущую позицию 2D
                double currentDist = i * stepLength;

                // Гарантируем, что последняя точка — это точно p1 (чтобы не вылететь за пределы или не не дойти)
                Vector2d currentPos2d;
                if (currentDist >= totalDistance)
                    currentPos2d = p1;
                else
                    currentPos2d = p0 + direction * currentDist;

                // Формируем луч
                Ray3d ray = new Ray3d(new Vector3d(currentPos2d.x, currentPos2d.y, rayOriginZ), -Vector3d.AxisZ);

                // Ищем пересечение
                int hitTID = spatialIndex.FindNearestHitTriangle(ray);
                if (hitTID != DMesh3.InvalidID)
                {
                    var intr = MeshQueries.TriangleIntersection(spatialIndex.Mesh, hitTID, ray);
                    Vector3d hitPoint = ray.PointAt(intr.RayParameter);

                    // Поднимаем на высоту фокуса
                    hitPoint.z += zOffset;

                    if(validHits > 0)
                        result.Add(new PathSegment<Vector3d>(lastHit, hitPoint, laserEnable));
                    lastHit = hitPoint;
                    validHits++;
                }
                // ELSE: Если лазер вышел за пределы детали (дырка или край), 
                // точку просто не добавляем. Лазер либо выключится, либо пройдет по прямой к следующей найденной точке.
            }

            return result;
        }

        private static List<PathSegment<Vector3d>> SimplifyTrace(List<PathSegment<Vector3d>> rawSegments, double tolerance)
        {
            if (rawSegments.Count < 2) return new List<PathSegment<Vector3d>>(rawSegments);

            var simplified = new List<PathSegment<Vector3d>>();

            PathSegment<Vector3d> current = rawSegments[0];
            
            for (int i = 1; i < rawSegments.Count; i++)
            {
                PathSegment<Vector3d> next = rawSegments[i];

                Vector3d prevDir = (current.p1 - current.p0).Normalized;
                Vector3d nextDir = (next.p1 - next.p0).Normalized;

                // Проверяем коллинеарность через Скалярное произведение (Dot Product).
                // Если векторы сонаправлены, Dot близко к 1.0.
                // 1.0 - Dot < tolerance означает, что угол очень мал.
                double dot = prevDir.Dot(nextDir);

                bool semidir = (1.0 - dot) < tolerance;
                bool laserSwitch = next.laserOn != current.laserOn;

                if(semidir && !laserSwitch)
                {
                    current = new PathSegment<Vector3d>(current.p0, next.p1, current.laserOn);
                }
                else
                {
                    simplified.Add(current);
                    current = next;
                }
            }

            simplified.Add(current);

            return simplified;
        }

        /*public static void AddMarginsX(List<g3.Vector3d> path3d, double xMargin)
        {
            for(int i = 0; i < path3d.Count; i++)
            {
                path3d[i] = new Vector3d()
            }
        }*/
    }
}
