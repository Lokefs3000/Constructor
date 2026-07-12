using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Primary.Collections.ReadOnly.Display
{
    internal sealed class ROListDisplay<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly T[] _values;

        public ROListDisplay(ROList<T> values)
        {
            _values = [.. values];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _values;
    }
}
