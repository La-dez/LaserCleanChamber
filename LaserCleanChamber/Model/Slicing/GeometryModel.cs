using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using g3;
using HelixToolkit.Wpf;

namespace LaserCleanChamber.Model.Slicing
{
    public class GeometryModel
    {
        /// <summary>
        /// Модель в формате geometry3Sharp для всех вычислений.
        /// </summary>
        public DMesh3 Mesh { get; private set; }
        public DMeshAABBTree3 SpatialIndex { get; init; }

        private GeometryModel(DMesh3 mesh)
        {
            Mesh = mesh;
            SpatialIndex = new DMeshAABBTree3(Mesh, true);
        }

        /// <summary>
        /// Статический метод для безопасной загрузки модели из файла (STL, OBJ).
        /// </summary>
        public static GeometryModel LoadFromFile(string filePath)
        {
            var mesh = StandardMeshReader.ReadMesh(filePath);
            
            if (mesh == null)
                throw new Exception("Cannot load mesh");
            return new GeometryModel(mesh);
        }

        public GeometryModel CreateFlippedVersion(bool aroundCenter = true)
        {
            // 1. Делаем глубокую копию меша, чтобы не испортить оригинал
            DMesh3 flippedMesh = new DMesh3(Mesh);

            // Опеределяем точку, вокруг которой будем крутить
            Vector3d pivot = aroundCenter ? flippedMesh.CachedBounds.Center : Vector3d.Zero;

            // 2. Выполняем поворот на 180 градусов вокруг оси X (или Y, смотря что у вас "верх")
            // AxisX = (1, 0, 0), 180 градусов.
            Quaterniond q = new Quaterniond(Vector3d.AxisY, 180);
            MeshTransforms.Rotate(flippedMesh, pivot, q);

            return new GeometryModel(flippedMesh);
        }
    }
}
