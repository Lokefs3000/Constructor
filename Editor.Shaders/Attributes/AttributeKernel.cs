using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Shaders.Attributes
{
    public sealed class AttributeKernel : AttributeSignature
    {
        public AttributeKernel() : base(
            "kernel",
            AttributeUsage.Function,
            Array.Empty<AttributeRelation>(),
            Array.Empty<AttributeVariable>())
        {
        }
    }
}
