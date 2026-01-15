using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Shaders.Attributes
{
    public sealed class AttributeNumThreads : AttributeSignature
    {
        public AttributeNumThreads() : base(
            "numthreads",
            AttributeUsage.Function,
            [new AttributeRelation(typeof(AttributeKernel), AttributeRelationFlags.Required)],
            s_variables)
        {
        }

        private static readonly AttributeVariable[] s_variables = [
            new AttributeVariable("X", typeof(int), null, AttributeFlags.Required),
            new AttributeVariable("Y", typeof(int), null, AttributeFlags.Required),
            new AttributeVariable("Z", typeof(int), null, AttributeFlags.Required),
            ];
    }
}
