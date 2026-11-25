using System;
using System.Collections.Generic;
using System.Text;
using NewShaderProcessor.Attributes.Builder;

namespace NewShaderProcessor.Attributes
{
    public sealed class Kernel : AttributeSignature
    {
        protected override AttributeBuilder Setup()
        {
            AttributeBuilder builder = new AttributeBuilder(
                Usage: AttributeUsage.Function);

            builder.Incompatible<Vertex>();
            builder.Incompatible<Pixel>();

            builder.AddArgument<string>("name");

            return builder;
        }
    }
}
