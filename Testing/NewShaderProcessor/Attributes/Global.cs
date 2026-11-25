using System;
using System.Collections.Generic;
using System.Text;
using NewShaderProcessor.Attributes.Builder;

namespace NewShaderProcessor.Attributes
{
    public sealed class Global : AttributeSignature
    {
        protected override AttributeBuilder Setup()
        {
            AttributeBuilder builder = new AttributeBuilder(
                Usage: AttributeUsage.Property | AttributeUsage.GenericResource);

            builder.Incompatible<Property>();

            builder.AddArgument<string>("name");

            return builder;
        }
    }
}
