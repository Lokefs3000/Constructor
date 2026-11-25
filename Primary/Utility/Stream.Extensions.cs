using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Common;

namespace Primary.Utility
{
    public static class StreamExtensions
    {
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

            public void ReadExactly<T>(Span<T> value) where T : unmanaged
            {
                stream.ReadExactly(MemoryMarshal.Cast<T, byte>(value));
            }

            public string ReadStringUtf8(int length)
            {
                if (length < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException("String length must be not be negative");
                if (length == 0)
                    return string.Empty;

                using RentedArray<byte> buffer = new RentedArray<byte>(length);
                stream.ReadExactly(buffer.Span);

                return Encoding.UTF8.GetString(buffer.Span);
            }

            public string ReadStringUtf16(int length)
            {
                if (length < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException("String length must be not be negative");
                if (length == 0)
                    return string.Empty;

                using RentedArray<char> buffer = new RentedArray<char>(length);
                stream.ReadExactly(buffer.Span);

                return buffer.Span.ToString();
            }

            public void Write<T>(ReadOnlySpan<T> value) where T : unmanaged
            {
                stream.Write(MemoryMarshal.Cast<T, byte>(value));
            }

            public void Skip(long bytesToSkip) => stream.Seek(bytesToSkip, SeekOrigin.Current);
            public void Skip<T>() where T : unmanaged => stream.Seek(Unsafe.SizeOf<T>(), SeekOrigin.Current);
            public void Skip<T>(int count) where T : unmanaged => stream.Seek(Unsafe.SizeOf<T>() * count, SeekOrigin.Current);
        }
    }
}
