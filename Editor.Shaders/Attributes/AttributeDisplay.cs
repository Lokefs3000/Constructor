using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Attributes
{
    public sealed class AttributeDisplay : AttributeSignature
    {
        public AttributeDisplay() : base(
            "display",
            AttributeUsage.Property,
            [],
            s_variables)
        {
        }

        private static readonly AttributeVariable[] s_variables = [
            new AttributeVariable("Display", typeof(PropertyDisplay), null, AttributeFlags.Required),
            ];
    }
}
