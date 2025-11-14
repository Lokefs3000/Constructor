using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Attributes
{
    internal sealed class AttributeBindGroup : AttributeSignature
    {
        public AttributeBindGroup() : base(
            "bindgroup",
            AttributeUsage.GenericResource,
            Array.Empty<AttributeRelation>(),
            s_variables)
        {
        }

        private static readonly AttributeVariable[] s_variables = [
            new AttributeVariable("Group", typeof(string), null, AttributeFlags.Required),
            ];
    }
}
