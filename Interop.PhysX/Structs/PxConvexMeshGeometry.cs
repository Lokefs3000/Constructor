using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxConvexMeshGeometry
    {
        public PxGeometry @base;
        public PxMeshScale scale;
        public PxConvexMesh* convexMesh;
        public PxConvexMeshGeometryFlag meshFlags;
    }
}
