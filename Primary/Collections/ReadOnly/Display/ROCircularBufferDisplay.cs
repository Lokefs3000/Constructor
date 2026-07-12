using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Primary.Collections.ReadOnly.Display
{
    internal sealed class ROCircularBufferDisplay<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly T[] _values;

        public ROCircularBufferDisplay(ROCircularBuffer<T> values)
        {
            _values = [.. values];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _values;
    }
}
