using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RHI = Primary.RHI;

namespace Editor.Shaders.Attributes
{
    internal sealed class AttributeIALayout : AttributeSignature
    {
        public AttributeIALayout() : base(
            "ialayout",
            AttributeUsage.Function,
            [new AttributeRelation(typeof(AttributeVertex), AttributeRelationFlags.Required)],
            s_variables)
        {
        }

        private static readonly AttributeVariable[] s_variables = [
            new AttributeVariable("Name", typeof(string), null, AttributeFlags.Required),
            new AttributeVariable("Offset", typeof(int), -1, AttributeFlags.None),
            new AttributeVariable("Slot", typeof(int), 0, AttributeFlags.None),
            new AttributeVariable("Class", typeof(RHI.InputClassification), null, AttributeFlags.None),
            new AttributeVariable("Format", typeof(RHI.InputElementFormat), null, AttributeFlags.None),
            ];
    }
}
