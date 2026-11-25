using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxHeightFieldGeometry
    {
        public PxGeometry @base;
        public PxHeightField* heightField;
        public float heightScale;
        public float rowScale;
        public float columnScale;
        public PxMeshGeometryFlag heightFieldFlags;
    }
}
