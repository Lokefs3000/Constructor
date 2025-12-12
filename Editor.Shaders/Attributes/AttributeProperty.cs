using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Attributes
{
    public sealed class AttributeProperty : AttributeSignature
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
            new AttributeVariable("Default", typeof(PropertyDefault), PropertyDefault.TexWhite, AttributeFlags.None),
            ];
    }
}
