using System;
using System.Collections.Generic;
using System.Text;

namespace NewShaderProcessor.Attributes.Builder
{
    public record struct AttributeBuilder(AttributeUsage Usage = AttributeUsage.None, bool AllowMultiple = false)
    {
        public readonly void Require<T>()
        {

        }

        public readonly void RequireAny<T>()
        {

        }

        public readonly void Incompatible<T>()
        {

        }

        public readonly void AddArgument<T>(string name, T? defaultValue = default, bool isRequired = false)
        {

        }
    }
}
