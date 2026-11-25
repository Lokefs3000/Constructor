using System;

namespace Interop.PhysX
{
    public enum PxActorType : int
    {
        eRIGID_STATIC = 0,
        eRIGID_DYNAMIC = 1,
        eARTICULATION_LINK = 2,
        eDEFORMABLE_SURFACE = 3,
        eDEFORMABLE_VOLUME = 4,
        ePBD_PARTICLESYSTEM = 5,
        eACTOR_COUNT = 6,
        eACTOR_FORCE_DWORD = 2147483647
    }
}
