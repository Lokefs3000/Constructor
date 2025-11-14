using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Common
{
    public static class ReadOnlySpanExtensions
    {
        public static int FindIndex<T>(this ReadOnlySpan<T> span, Predicate<T> predicate)
        {
            if (span.IsEmpty)
                return -1;

            int index = 0;

            do
            {
                if (predicate(span.DangerousGetReferenceAt(index)))
                    return index;
            } while (++index < span.Length);

            return -1;
        }
    }
}
