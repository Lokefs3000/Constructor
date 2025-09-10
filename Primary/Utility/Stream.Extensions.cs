using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Utility
{
    public static class StreamExtensions
    {
        public static T Read<T>(this Stream stream) where T : unmanaged
        {
            T v = default;
            stream.ReadExactly(MemoryMarshal.Cast<T, byte>(new Span<T>(ref v)));

            return v;
        }
	}
}
