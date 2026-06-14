using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Serialization.Arguments
{
    internal sealed class ArgumentFile
    {
        public List<ArgumentFormat> Arguments { get; set; } = [];
        public List<string> Includes { get; set; } = [];
    }
}
