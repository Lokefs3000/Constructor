using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Mathematics;

namespace Primary.Rendering.Batching
{
    public record struct BatchingOptions()
    {
        public Vector3 Center = Vector3.Zero;

        public Frustrum? ViewFrustrum = null;

        public ulong AllowedLayers = ulong.MaxValue;
    }
}
