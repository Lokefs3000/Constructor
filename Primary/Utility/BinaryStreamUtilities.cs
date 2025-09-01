using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Utility
{
    public static class BinaryStreamUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Read<T>(this BinaryReader br, T* pointer, int length) where T : unmanaged
        {
            br.Read(MemoryMarshal.Cast<T, byte>(new Span<T>(pointer, length)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Read<T>(this BinaryReader br, Span<T> span) where T : unmanaged
        {
            br.Read(MemoryMarshal.Cast<T, byte>(span));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Read<T>(this BinaryReader br, T[] array) where T : unmanaged
        {
            br.Read(MemoryMarshal.Cast<T, byte>(array.AsSpan()));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Skip(this BinaryReader br, int by)
        {
            br.BaseStream.Seek(by, SeekOrigin.Current);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T Read<T>(this BinaryReader br) where T : unmanaged
        {
            T v = default(T);
            br.Read(new Span<byte>(&v, sizeof(T)));

            return v;
        }
    }
}
