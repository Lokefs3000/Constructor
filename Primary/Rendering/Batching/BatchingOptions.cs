using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Assets;
using Primary.Mathematics;

namespace Primary.Rendering.Batching
{
    public record struct BatchingOptions()
    {
        public Vector3 Origin = Vector3.Zero;
        public float MaxViewingDistance = 1000.0f;

        public Frustrum ViewFrustrum = default;

        public MaterialShaderType ShaderType = MaterialShaderType.Default;
        public ulong AllowedLayers = ulong.MaxValue;
    }
}
