using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Attributes
{
    public sealed class AttributePixel : AttributeSignature
    {
        public AttributePixel() : base(
            "pixel",
            AttributeUsage.Function,
            [new AttributeRelation(typeof(AttributeVertex), AttributeRelationFlags.Incompatible)],
            Array.Empty<AttributeVariable>())
        {
        }
    }
}
