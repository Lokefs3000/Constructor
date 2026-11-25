using System;
using System.Collections.Generic;
using System.Text;
using NewShaderProcessor.Attributes.Builder;

namespace NewShaderProcessor.Attributes
{
    public sealed class NumThreads : AttributeSignature
    {
        protected override AttributeBuilder Setup()
        {
            AttributeBuilder builder = new AttributeBuilder(
                Usage: AttributeUsage.Function);

            builder.Require<Kernel>();

            builder.AddArgument<int>("x", 1);
            builder.AddArgument<int>("y", 1);
            builder.AddArgument<int>("z", 1);

            return builder;
        }
    }
}
