using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;

namespace Primary.Utility
{
    public static class StreamExtensions
    {
        //public static T Read<T>(this Stream stream) where T : unmanaged
        //{
        //    T v = default;
        //    stream.ReadExactly(MemoryMarshal.Cast<T, byte>(new Span<T>(ref v)));
        //
        //    return v;
        //}
        //
        //public static void Write<T>(this Stream stream, in T value) where T : unmanaged
        //{
        //    stream.Write(MemoryMarshal.Cast<T, byte>(new ReadOnlySpan<T>(in value)));
        //}

        public static unsafe T[] ReadArray<T>(this Stream stream, int count) where T : unmanaged
        {
            if (count == 0)
                return Array.Empty<T>();

            T[] array = new T[count];
            fixed (T* ptr = array)
            {
                stream.ReadExactly(new Span<byte>(ptr, sizeof(T) * array.Length));
            }

            return array;
        }

        extension (Stream stream)
        {
            public bool TryRead<T>(out T value) where T : unmanaged
            {
                if (!stream.CanRead || stream.Position + Unsafe.SizeOf<T>() > stream.Length)
                {
                    value = default;
                    return false;
                }

                value = stream.Read<T>();
                return true;
            }

            public int Read<T>(Span<T> value) where T : unmanaged
            {
                return stream.Read(MemoryMarshal.Cast<T, byte>(value));
            }

            public void ReadExactly<T>(Span<T> value) where T : unmanaged
            {
                stream.ReadExactly(MemoryMarshal.Cast<T, byte>(value));
            }

            public void Write<T>(ReadOnlySpan<T> value) where T : unmanaged
            {
                stream.Write(MemoryMarshal.Cast<T, byte>(value));
            }
        }
    }
}
