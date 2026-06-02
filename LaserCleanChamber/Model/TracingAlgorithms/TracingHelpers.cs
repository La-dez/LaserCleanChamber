using g3;
using LaserCleanChamber.Model.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserCleanChamber.Model.TracingAlgorithms
{
    public static class TracingHelpers
    {
        private static double Hypot(double x, double y, double z = 0) => Math.Sqrt(x * x + y * y + z * z);

        public static List<PathSegment<Vector3d>> ExtrapolateLineX(List<PathSegment<Vector3d>> traceResult, double xMargins, AxisAlignedBox2d bounds2D)
        {

            var lastSegment = traceResult.Last();
            var firstSegment = traceResult.First();
            bool invertionCoef = firstSegment.p1.x > firstSegment.p0.x;
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
            double L_ab = Hypot(a, b);
            double L_bc = Hypot(b, c);
            double L_ca = Hypot(c, a);

            double x = L_ab == 0 ? 0 : (a * b) / L_ab;
            double y = L_bc == 0 ? 0 : (b * c) / L_bc;
            double z = L_ca == 0 ? 0 : (c * a) / L_ca;

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

        public static List<PathSegment<Vector3d>> OrtoApproxSegment(PathSegment<Vector3d> segment, double h_min)
        {
            List<PathSegment<Vector3d>> path = new List<PathSegment<Vector3d>>();
            Vector3d dist = segment.p1 - segment.p0;
            //double h = Shortest(dist.x, dist.y, dist.z);

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

        public static List<PathSegment<Vector3d>> OrtoApproxPath(List<PathSegment<Vector3d>> path, double h_min = 3)
        {
            var result = new List<PathSegment<Vector3d>>();
            for (int i = 0; i < path.Count; i++)
            {
                if (path[i].laserOn)
                {
                    var ortoSeg = TracingHelpers.OrtoApproxSegment(path[i], h_min);
                    result.AddRange(ortoSeg);
                }
                else
                {
                    result.Add(path[i]);
                }
            }
            return result;
        }


        public static void InsertInactiveSegments(List<PathSegment<Vector3d>> path3d)
        {
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
        }


        public static List<PathSegment<Vector3d>> TraceAlongLine(DMeshAABBTree3 spatialIndex, AxisAlignedBox3d bounds,
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

                    if (validHits > 0)
                        result.Add(new PathSegment<Vector3d>(lastHit, hitPoint, laserEnable));
                    lastHit = hitPoint;
                    validHits++;
                }
                // ELSE: Если лазер вышел за пределы детали (дырка или край), 
                // точку просто не добавляем. Лазер либо выключится, либо пройдет по прямой к следующей найденной точке.
            }

            return result;
        }

        public static List<PathSegment<Vector3d>> SimplifyTrace(List<PathSegment<Vector3d>> rawSegments, double tolerance)
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

                if (semidir && !laserSwitch)
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
    }
}
