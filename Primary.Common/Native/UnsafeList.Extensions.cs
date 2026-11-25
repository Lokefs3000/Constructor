using Arch.LowLevel;
using CommunityToolkit.HighPerformance;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Common.Native
{
    public static unsafe partial class Extensions
    {
        public static void DangerousAddUnchecked<T>(this UnsafeList<T> @this, T value) where T : unmanaged
        {
            ref UnsafeArray<T> intArray = ref Unsafe.AsRef<UnsafeArray<T>>((UnsafeArray<T>*)&@this);
            ref int intCount = ref Unsafe.AsRef<int>((int*)((byte*)(&@this) + sizeof(UnsafeArray<T>)));

#if DEBUG
            if (intArray.Length == intCount)
                throw new ArgumentOutOfRangeException();
#endif
            intArray[intCount] = value;
            (*(int*)Unsafe.AsPointer(ref intCount))++;
        }

        public static void AddRange<T>(this UnsafeList<T> @this, ReadOnlySpan<T> values) where T : unmanaged
        {
            if (@this.Count + values.Length > @this.Capacity)
            {
                @this.EnsureCapacity((int)BitOperations.RoundUpToPowerOf2((uint)(@this.Count + values.Length)));
            }

            ref T last = ref Unsafe.Add(ref @this.AsSpan().DangerousGetReference(), @this.Count);
            values.CopyTo(MemoryMarshal.CreateSpan(ref last, @this.Capacity - @this.Count));

            //HACK: actually terrible
            *(int*)(((byte*)&@this) + Unsafe.SizeOf<UnsafeArray<T>>()) += values.Length;
        }
    }
}
