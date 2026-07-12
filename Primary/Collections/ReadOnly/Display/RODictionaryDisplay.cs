using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Primary.Collections.ReadOnly.Display
{
    internal sealed class RODictionaryDisplay<TKey, TValue> where TKey : notnull
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly KeyValuePair<TKey, TValue>[] _values;

        public RODictionaryDisplay(RODictionary<TKey, TValue> values)
        {
            _values = [.. values];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue>[] Items => _values;
    }
}
