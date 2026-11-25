using System;
using System.Collections.Generic;
using System.Text;

namespace PhysXBindGen.Exceptions
{
    internal sealed class StateNotFoundException : Exception
    {
        public StateNotFoundException()
        {
        }

        public StateNotFoundException(string? message) : base(message)
        {
        }
    }
}
