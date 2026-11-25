using System;

namespace Interop.PhysX
{
    public enum PxActorFlag : sbyte
    {
        eVISUALIZATION = 1,
        eDISABLE_GRAVITY = 2,
        eSEND_SLEEP_NOTIFIES = 4,
        eDISABLE_SIMULATION = 8
    }
}
