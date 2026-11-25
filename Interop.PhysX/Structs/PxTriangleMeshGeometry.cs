using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxTriangleMeshGeometry
    {
        public PxGeometry @base;
        public PxMeshScale scale;
        public PxMeshGeometryFlag meshFlags;
        public PxTriangleMesh* triangleMesh;
    }
}
