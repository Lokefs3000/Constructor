using System;

namespace Interop.PhysX
{
    public enum PxHitFlag : ushort
    {
        ePOSITION = 1,
        eNORMAL = 2,
        eUV = 8,
        eASSUME_NO_INITIAL_OVERLAP = 16,
        eANY_HIT = 32,
        eMESH_MULTIPLE = 64,
        eMESH_BOTH_SIDES = 128,
        ePRECISE_SWEEP = 256,
        eMTD = 512,
        eFACE_INDEX = 1024,
        eDEFAULT = 3,
        eMODIFIABLE_FLAGS = 208
    }
}
