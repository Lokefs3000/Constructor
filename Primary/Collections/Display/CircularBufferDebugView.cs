using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Primary.Collections.Display
{
    internal sealed class CircularBufferDebugView<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly CircularBuffer<T> _values;

        public CircularBufferDebugView(CircularBuffer<T> values)
        {
            _values = values;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                return [.. _values];
            }
        }
    }
}
