using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxBroadPhaseRegion
    {
        public PxBounds3 mBounds;
        public void* mUserData;
    }
}
