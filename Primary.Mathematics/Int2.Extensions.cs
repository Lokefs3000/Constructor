using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Primary.Mathematics
{
    public static partial class Extensions
    {
        public static Vector128<int> AsVector128Unsafe(this Int2 value)
        {
            Unsafe.SkipInit(out Vector128<int> result);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref result), value);
            return result;
        }

        public static Vector128<int> AsVector128(this Int2 value)
        {
            Vector128<int> result = Vector128<int>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref result), value);
            return result;
        }

        public static Vector128<int> AsVector128(this Int2 value, int defaultValue)
        {
            Vector128<int> result = Vector128.Create(defaultValue);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref result), value);
            return result;
        }

        public static Int2 AsInt2(this Vector128<int> value)
        {
            ref byte address = ref Unsafe.As<Vector128<int>, byte>(ref value);
            return Unsafe.ReadUnaligned<Int2>(ref address);
        }

        public static Vector2 AsVector2(this Int2 value) => new Vector2(value.X, value.Y);

        public static Int2 AsInt2(this Vector2 value) => new Int2((int)value.X, (int)value.Y);
    }
}
