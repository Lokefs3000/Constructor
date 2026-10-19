using System;
using System.Collections.Generic;
using System.Text;
using NewShaderProcessor.Attributes.Builder;

namespace NewShaderProcessor.Attributes
{
    public sealed class Sampled : AttributeSignature
    {
        protected override AttributeBuilder Setup()
        {
            AttributeBuilder builder = new AttributeBuilder(
                Usage: AttributeUsage.GenericTexture);

            builder.Require<Property>();

            builder.AddArgument<string>("samplerName", isRequired: true);

            return builder;
        }
    }
}
