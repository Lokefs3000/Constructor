using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Editor.Processors
{
    internal static unsafe class BinaryUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Write<T>(this BinaryWriter writer, T value) where T : unmanaged
        {
            if (sizeof(T) == 1)
                writer.Write(Unsafe.As<T, byte>(ref value));
            else
                writer.Write(new ReadOnlySpan<byte>(&value, sizeof(T)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Write<T>(this BinaryWriter writer, ReadOnlySpan<T> value) where T : unmanaged
        {
            writer.Write(MemoryMarshal.Cast<T, byte>(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Write<T>(this BinaryWriter writer, Span<T> value) where T : unmanaged
        {
            writer.Write(MemoryMarshal.Cast<T, byte>(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Write<T>(this BinaryWriter writer, ReadOnlyMemory<T> value) where T : unmanaged
        {
            writer.Write(MemoryMarshal.Cast<T, byte>(value.Span));
        }
    }
}
