using System;
using System.Collections.Generic;
using System.Text;
using NewShaderProcessor.Attributes.Builder;

namespace NewShaderProcessor.Attributes
{
    public sealed class Constants : AttributeSignature
    {
        protected override AttributeBuilder Setup()
        {
            AttributeBuilder builder = new AttributeBuilder(
                Usage: AttributeUsage.ConstantBuffer);

            return builder;
        }
    }
}
