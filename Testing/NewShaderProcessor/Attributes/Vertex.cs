using System;
using System.Collections.Generic;
using System.Text;
using NewShaderProcessor.Attributes.Builder;

namespace NewShaderProcessor.Attributes
{
    public sealed class Vertex : AttributeSignature
    {
        protected override AttributeBuilder Setup()
        {
            AttributeBuilder builder = new AttributeBuilder(
                Usage: AttributeUsage.Function);

            builder.Incompatible<Pixel>();
            builder.Incompatible<Kernel>();
            return builder;
        }
    }
}
