using System.Diagnostics;

namespace Primary.SourceGenerator.Utility.Display
{
    internal sealed class RentedListDebugView<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly T[] _values;

        public RentedListDebugView(RentedList<T> values)
        {
            _values = [.. values];
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _values;
    }
}
