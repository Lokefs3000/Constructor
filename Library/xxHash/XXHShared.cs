using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using xxHash.Int32;

namespace xxHash
{
    public static unsafe class XXHShared
    {
        public static void* Malloc(ulong s)
        {
            return NativeMemory.Alloc((nuint)s);
        }

        public static void Free(void* p)
        {
            NativeMemory.Free(p);
        }

        public static void Memcpy(void* dest, void* src, ulong count)
        {
            NativeMemory.Copy(src, dest, (nuint)count);
        }

        public static void Memset(void* dest, int ch, ulong count)
        {
            NativeMemory.Fill(dest, (nuint)count, (byte)ch);
        }

        public static int Memcmp(void* lhs, void* rhs, ulong count)
        {
            Debug.Assert(count < int.MaxValue, "Count is larger than >2 gb! If this is an issue change out the span to something else in the 'xxHash.XXHShared.Memcmp' method");
            return new Span<byte>(lhs, (int)count).SequenceCompareTo(new Span<byte>(rhs, (int)count));
        }

        // currently this is the 'ForceMemoryAccess == 0' options
        public static uint XXHRead32(void* memPtr)
        {
            uint val;
            Memcpy(&val, memPtr, sizeof(uint));
            return val;
        }

        public static bool IsLittleEndian()
        {
            return BitConverter.IsLittleEndian;
        }

        public static uint XXHRotl32(uint x, int r)
        {
            return BitOperations.RotateLeft(x, r);
        }

        public static ulong XXHRotl64(ulong x, int r)
        {
            return BitOperations.RotateLeft(x, r);
        }

        public static uint XXHSwap32(uint x)
        {
            return BinaryPrimitives.ReverseEndianness(x);
        }

        public static uint XXHReadLE32(void* ptr)
        {
            return IsLittleEndian() ? XXHRead32(ptr) : XXHSwap32(XXHRead32(ptr));
        }

        public static uint XXHReadBE32(void* ptr)
        {
            return IsLittleEndian() ? XXHSwap32(XXHRead32(ptr)) : XXHRead32(ptr);
        }

        public static uint ReadLE32Align(void* ptr, XXHAlignment align)
        {
            if (align == XXHAlignment.Unaligned)
                return XXHReadLE32(ptr);
            else
                return IsLittleEndian() ? *(uint*)ptr : XXHSwap32(*(uint*)ptr);
        }
    }
}
