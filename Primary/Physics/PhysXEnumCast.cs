using System;
using System.Collections.Generic;
using System.Text;
using Interop.PhysX;
using Primary.Common;
using Primary.Components;

namespace Primary.Physics
{
    internal static class PhysXEnumCast
    {
        public static PxShapeFlag GetShapeFlags(ColliderUsage usage)
        {
            PxShapeFlag flags = 0;

            if (usage.HasFlags(ColliderUsage.Simulation))
                flags |= PxShapeFlag.eSIMULATION_SHAPE;
            if (usage.HasFlags(ColliderUsage.SceneQuery))
                flags |= PxShapeFlag.eSCENE_QUERY_SHAPE;
            if (usage.HasFlags(ColliderUsage.Trigger))
                flags |= PxShapeFlag.eTRIGGER_SHAPE;

            return flags;
        }
    }
}
