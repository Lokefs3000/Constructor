using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Primary.Collections.Display
{
    internal sealed class DynamicCircularBufferDebugView<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly DynamicCircularBuffer<T> _values;

        public DynamicCircularBufferDebugView(DynamicCircularBuffer<T> values)
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
