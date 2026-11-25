using System;

namespace Interop.PhysX
{
    public enum PxQueryFlag : ushort
    {
        eSTATIC = 1,
        eDYNAMIC = 2,
        ePREFILTER = 4,
        ePOSTFILTER = 8,
        eANY_HIT = 16,
        eNO_BLOCK = 32,
        eBATCH_QUERY_LEGACY_BEHAVIOUR = 64,
        eDISABLE_HARDCODED_FILTER = 64,
        eRESERVED = 32768
    }
}
