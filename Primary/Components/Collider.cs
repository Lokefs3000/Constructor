using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Primary.Components
{
    public abstract class Collider : EntityBehaviour
    {
        protected ColliderUsage _colliderUsage;

        protected Vector3 _centerOffset;
    }

    [Flags]
    public enum ColliderUsage : byte
    {
        Simulation = 1 << 0,
        SceneQuery = 1 << 1,
        Trigger = 1 << 2
    }
}
