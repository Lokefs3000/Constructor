using Arch.LowLevel;
using System.Runtime.CompilerServices;

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
    }
}
