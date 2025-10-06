﻿using System.Runtime.InteropServices;

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

        public static void Write<T>(this Stream stream, in T value) where T : unmanaged
        {
            stream.Write(MemoryMarshal.Cast<T, byte>(new ReadOnlySpan<T>(in value)));
        }
    }
}
