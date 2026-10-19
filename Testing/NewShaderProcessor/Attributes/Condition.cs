using System;
using System.Collections.Generic;
using System.Text;
using NewShaderProcessor.Attributes.Builder;

namespace NewShaderProcessor.Attributes
{
    public sealed class Condition : AttributeSignature
    {
        protected override AttributeBuilder Setup()
        {
            AttributeBuilder builder = new AttributeBuilder(
                Usage: AttributeUsage.Function,
                AllowMultiple: true);

            builder.RequireAny<Vertex>();
            builder.RequireAny<Pixel>();

            builder.AddArgument<string>("switchName", isRequired: true);

            return builder;
        }
    }
}
