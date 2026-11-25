using System;

namespace Interop.PhysX
{
    public enum PxDynamicTreeSecondaryPruner : int
    {
        eNONE = 0,
        eBUCKET = 1,
        eINCREMENTAL = 2,
        eBVH = 3,
        eLAST = 4
    }
}
