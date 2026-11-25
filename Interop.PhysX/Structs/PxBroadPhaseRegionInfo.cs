using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxBroadPhaseRegionInfo
    {
        public PxBroadPhaseRegion mRegion;
        public uint mNbStaticObjects;
        public uint mMbDynamicObjects;
        public bool mActive;
        public bool mOverlap;
    }
}
