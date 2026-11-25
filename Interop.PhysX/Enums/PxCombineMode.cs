using System;

namespace Interop.PhysX
{
    public enum PxCombineMode : uint
    {
        eAVERAGE = 0,
        eMIN = 1,
        eMULTIPLY = 2,
        eMAX = 3,
        eN_VALUES = 4,
        ePAD_32 = 2147483647
    }
}
