using System;

namespace Interop.PhysX
{
    public enum PxConstraintFlag : ushort
    {
        eBROKEN = 1,
        eCOLLISION_ENABLED = 8,
        eVISUALIZATION = 16,
        eDRIVE_LIMITS_ARE_FORCES = 32,
        eIMPROVED_SLERP = 128,
        eDISABLE_PREPROCESSING = 256,
        eENABLE_EXTENDED_LIMITS = 512,
        eGPU_COMPATIBLE = 1024,
        eALWAYS_UPDATE = 2048,
        eDISABLE_CONSTRAINT = 4096
    }
}
