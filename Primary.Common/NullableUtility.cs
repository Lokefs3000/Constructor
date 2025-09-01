using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Primary.Common
{
    public static class NullableUtility
    {
        [DebuggerStepThrough, DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ThrowIfNull<T>(in T? value)
        {
#if DEBUG
            if (value == null)
                throw new NullReferenceException(nameof(value));
#endif
            return value!;
        }

        [DebuggerStepThrough, DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AlwaysThrowIfNull<T>(in T? value)
        {
            if (value == null)
                throw new NullReferenceException(nameof(value));
            return value!;
        }

        public static bool GetIfNotNull<T>(in T? value, out T result)
        {
            if (value != null)
            {
                result = value;
                return true;
            }

            Unsafe.SkipInit(out result);
            return false;
        }
    }
}
