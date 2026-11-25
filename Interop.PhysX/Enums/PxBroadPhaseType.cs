using System;

namespace Interop.PhysX
{
    public enum PxBroadPhaseType : int
    {
        eSAP = 0,
        eMBP = 1,
        eABP = 2,
        ePABP = 3,
        eGPU = 4,
        eLAST = 5
    }
}
