using System;
using System.Collections.Generic;
using System.Text;

namespace PhysXBindGen.Exceptions
{
    internal sealed class DuplicateStateException : Exception
    {
        public DuplicateStateException()
        {
        }

        public DuplicateStateException(string? message) : base(message)
        {
        }
    }
}
