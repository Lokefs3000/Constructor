using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Primary.Collections.ReadOnly.Display
{
    internal sealed class ROSortedListDisplay<TKey, TValue> where TKey : notnull
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly KeyValuePair<TKey, TValue>[] _values;

        public ROSortedListDisplay(ROSortedList<TKey, TValue> values)
        {
            _values = [.. values];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue>[] Items => _values;
    }
}
