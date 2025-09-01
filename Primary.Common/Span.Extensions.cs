using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Common
{
    public static class SpanExtensions
    {
        public static bool Equals(this Span<char> @this, in ReadOnlySpan<char> value, StringComparison comparisonType)
            => ((ReadOnlySpan<char>)@this).Equals(value, comparisonType);
    }
}
