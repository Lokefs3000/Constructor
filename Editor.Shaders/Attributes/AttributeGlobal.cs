using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Shaders.Attributes
{
    internal sealed class AttributeGlobal : AttributeSignature
    {
        public AttributeGlobal() : base(
            "global",
            AttributeUsage.Property | AttributeUsage.GenericResource,
            Array.Empty<AttributeRelation>(),
            s_variables)
        {
        }

        private static readonly AttributeVariable[] s_variables = [
            new AttributeVariable("Name", typeof(string), null, AttributeFlags.None),
            ];
    }
}
