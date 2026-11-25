using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Serialization.Arguments
{
    internal sealed class ArgumentFormat
    {
        public string Name { get; set; } = string.Empty;
        public string? Shorthand { get; set; } = null;
        public ArgumentType Type { get; set; } = default;
    }
}
