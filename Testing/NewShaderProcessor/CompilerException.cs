using System;
using System.Collections.Generic;
using System.Text;

namespace NewShaderProcessor
{
    public sealed class CompilerException : Exception
    {
        public CompilerException()
        {
        }

        public CompilerException(string? message) : base(message)
        {
        }
    }
}
