using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Attributes
{
    internal sealed class AttributeConstants : AttributeSignature
    {
        public AttributeConstants() : base(
            "constants",
            AttributeUsage.ConstantBuffer,
            Array.Empty<AttributeRelation>(),
            Array.Empty<AttributeVariable>())
        {
        }
    }
}
