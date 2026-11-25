using System;

namespace Interop.PhysX
{
    public enum PxShapeFlag : int
    {
        eSIMULATION_SHAPE = 1,
        eSCENE_QUERY_SHAPE = 2,
        eTRIGGER_SHAPE = 4,
        eVISUALIZATION = 8
    }
}
