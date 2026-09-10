using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Primary.Collections.Display
{
    internal sealed class RentedStackDebugView<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly T[] _values;

        public RentedStackDebugView(RentedStack<T> values)
        {
            _values = [.. values];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _values;
    }
}
