using System;
using System.Collections.Generic;
using System.Text;

namespace PhysXBindGen.Exceptions
{
    internal sealed class SourceException : Exception
    {
        public SourceException()
        {
        }

        public SourceException(string? message) : base(message)
        {
        }
    }
}
