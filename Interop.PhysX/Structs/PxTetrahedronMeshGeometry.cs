using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxTetrahedronMeshGeometry
    {
        public PxGeometry @base;
        public PxTetrahedronMesh* tetrahedronMesh;
    }
}
