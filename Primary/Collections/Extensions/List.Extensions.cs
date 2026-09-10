using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;

namespace Primary.Collections.Extensions
{
    public static partial class Extensions
    {
        extension<T> (List<T> list)
        {
            /// <summary>Get a reference to a value stored in a list with bounds checking</summary>
            /// <param name="i">Index to get reference for</param>
            /// <returns>Reference of the object stored in the <seealso cref="List{T}"/> at <paramref name="i"/></returns>
            /// <remarks>
            ///     This internally calls <seealso cref="ListExtensions.AsSpan{T}(List{T}?)"/> and indexes into the span instead.<br/>
            ///     This is intended to be a way to modify a value type without getting it first.<br/><br/>
            ///     <b>THE REFERENCE BECOMES INVALID THE MOMENT THE LIST SWAPS ITS INTERNAL ARRAY SO KEEP IT ONLY FOR SHORT PERIODS OF CONTROLLED USAGE</b>
            /// </remarks>
            public ref T GetRefAtIndex(int i)
            {
                return ref list.AsSpan()[i];
            }
        }
    }
}
