using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Windows.GCProfiler
{
    public interface IGraphSource<T>
    {
        public T Maximum { get; }
        public T Minimum { get; }

        public ReadOnlySpan<T> Values { get; }
        public int Head { get; }
    }
}
