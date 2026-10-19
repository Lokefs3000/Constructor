using System;
using System.Collections.Generic;
using System.Text;
using NewShaderProcessor.Attributes.Builder;
using Primary.RHI;

namespace NewShaderProcessor.Attributes
{
    public sealed class IALayout : AttributeSignature
    {
        protected override AttributeBuilder Setup()
        {
            AttributeBuilder builder = new AttributeBuilder(
                Usage: AttributeUsage.Function,
                AllowMultiple: true);

            builder.Require<Vertex>();

            builder.AddArgument<string>("name", isRequired: true);
            builder.AddArgument<int>("offset", -1);
            builder.AddArgument<int>("slot", 0);
            builder.AddArgument<RHIInputClass>("class", RHIInputClass.PerVertex);
            builder.AddArgument<RHIElementFormat?>("format", null);

            return builder;
        }
    }
}
