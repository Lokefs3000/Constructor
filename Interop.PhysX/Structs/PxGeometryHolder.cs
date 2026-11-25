using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct PxGeometryHolder
    {
        [FieldOffset(0)]
        public PxGeometry @base;
        [FieldOffset(0)]
        public PxBoxGeometry box;
        [FieldOffset(0)]
        public PxCapsuleGeometry capsule;
        [FieldOffset(0)]
        public PxConvexCoreGeometry convexCore;
        [FieldOffset(0)]
        public PxConvexMeshGeometry convexMesh;
        [FieldOffset(0)]
        public PxHeightFieldGeometry heightField;
        [FieldOffset(0)]
        public PxParticleSystemGeometry particleSystem;
        [FieldOffset(0)]
        public PxPlaneGeometry plane;
        [FieldOffset(0)]
        public PxSphereGeometry sphere;
        [FieldOffset(0)]
        public PxTetrahedronMeshGeometry tetrahedronMesh;
        [FieldOffset(0)]
        public PxTriangleMeshGeometry triangleMesh;
    }
}
