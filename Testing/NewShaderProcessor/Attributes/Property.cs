using System;
using System.Collections.Generic;
using System.Text;
using NewShaderProcessor.Attributes.Builder;

namespace NewShaderProcessor.Attributes
{
    public sealed class Property : AttributeSignature
    {
        protected override AttributeBuilder Setup()
        {
            AttributeBuilder builder = new AttributeBuilder(
                Usage: AttributeUsage.GenericTexture | AttributeUsage.Property);

            builder.AddArgument<string>("name");
            builder.AddArgument<PropertyDefault>("default", PropertyDefault.TexWhite);

            return builder;
        }
    }
}
