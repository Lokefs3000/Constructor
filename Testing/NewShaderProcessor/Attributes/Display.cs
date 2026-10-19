using System;
using System.Collections.Generic;
using System.Text;
using NewShaderProcessor.Attributes.Builder;

namespace NewShaderProcessor.Attributes
{
    public sealed class Display : AttributeSignature
    {
        protected override AttributeBuilder Setup()
        {
            AttributeBuilder builder = new AttributeBuilder(
                Usage: AttributeUsage.Property);

            builder.Require<Property>();

            builder.AddArgument<PropertyDisplay>("display", PropertyDisplay.Default, isRequired: true);

            return builder;
        }
    }
}
