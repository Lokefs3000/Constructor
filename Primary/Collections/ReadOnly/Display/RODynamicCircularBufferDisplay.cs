using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Primary.Collections.ReadOnly.Display
{
    internal sealed class RODynamicCircularBufferDisplay<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly T[] _values;

        public RODynamicCircularBufferDisplay(RODynamicCircularBuffer<T> values)
        {
            _values = [.. values];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _values;
    }
}
