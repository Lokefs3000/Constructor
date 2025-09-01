using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Common
{
    //TODO: replace internal functions with "Unsafe.BitCast" instead
    public static class FlagUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool HasFlag<TEnum>(TEnum value, TEnum flags) where TEnum : unmanaged
        {
            switch (sizeof(TEnum))
            {
                case 1: return (*(byte*)&value & *(byte*)&flags) == *(byte*)&flags;
                case 2: return (*(ushort*)&value & *(ushort*)&flags) == *(ushort*)&flags;
                case 4: return (*(uint*)&value & *(uint*)&flags) == *(uint*)&flags;
                case 8: return (*(ulong*)&value & *(ulong*)&flags) == *(ulong*)&flags;
                default: return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool HasEither<TEnum>(TEnum value, TEnum flags) where TEnum : unmanaged
        {
            switch (sizeof(TEnum))
            {
                case 1: return (*(byte*)&value & *(byte*)&flags) > 0;
                case 2: return (*(ushort*)&value & *(ushort*)&flags) > 0;
                case 4: return (*(uint*)&value & *(uint*)&flags) > 0;
                case 8: return (*(ulong*)&value & *(ulong*)&flags) > 0;
                default: return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TEnum AddFlags<TEnum>(TEnum value, TEnum flags) where TEnum : unmanaged
        {
            switch (sizeof(TEnum))
            {
                case 1: return Unsafe.BitCast<byte, TEnum>((byte)(*(byte*)&value | *(byte*)&flags));
                case 2: return Unsafe.BitCast<ushort, TEnum>((ushort)(*(ushort*)&value | *(ushort*)&flags));
                case 4: return Unsafe.BitCast<uint, TEnum>((uint)(*(uint*)&value | *(uint*)&flags));
                case 8: return Unsafe.BitCast<ulong, TEnum>((ulong)(*(ulong*)&value | *(ulong*)&flags));
                default: return value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TEnum RemoveFlags<TEnum>(TEnum value, TEnum flags) where TEnum : unmanaged
        {
            switch (sizeof(TEnum))
            {
                case 1: return Unsafe.BitCast<byte, TEnum>((byte)(*(byte*)&value & ~*(byte*)&flags));
                case 2: return Unsafe.BitCast<ushort, TEnum>((ushort)(*(ushort*)&value & ~*(ushort*)&flags));
                case 4: return Unsafe.BitCast<uint, TEnum>((uint)(*(uint*)&value & ~*(uint*)&flags));
                case 8: return Unsafe.BitCast<ulong, TEnum>((ulong)(*(ulong*)&value & ~*(ulong*)&flags));
                default: return value;
            }
        }
    }
}
