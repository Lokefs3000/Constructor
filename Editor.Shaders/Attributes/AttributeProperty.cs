using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Attributes
{
    internal sealed class AttributeProperty : AttributeSignature
    {
        public AttributeProperty() : base(
            "property",
            AttributeUsage.GenericTexture,
            Array.Empty<AttributeRelation>(),
            s_variables)
        {
        }

        private static readonly AttributeVariable[] s_variables = [
            new AttributeVariable("Name", typeof(string), null, AttributeFlags.None),
            new AttributeVariable("Default", typeof(PropertyDefault), PropertyDefault.White, AttributeFlags.None),
            ];
    }
}
