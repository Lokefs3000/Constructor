using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geometry.Mesh
{
    public sealed class BrushMeshCache
    {
        private readonly BrushMeshGenerator _selfGenerator;

        private ConcurrentDictionary<BrushId, BrushMesh> _meshes;

        internal BrushMeshCache()
        {
            _selfGenerator = new BrushMeshGenerator();

            _meshes = new ConcurrentDictionary<BrushId, BrushMesh>();
        }

        /// <summary>Not thread-safe</summary>
        public BrushMesh GetMesh(Brush brush)
        {
            bool hasMesh = _meshes.TryGetValue(brush.Id, out BrushMesh mesh);
            if (!hasMesh || mesh.UpdateIndex != brush.UpdateIndex)
            {
                mesh = _selfGenerator.UpdateBrushMesh(brush, hasMesh ? mesh : null);
                _meshes[brush.Id] = mesh;
            }

            return mesh;
        }
    }
}
