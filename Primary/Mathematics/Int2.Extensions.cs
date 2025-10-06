using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Primary.Mathematics
{
    public static partial class VecInt
    {
        public static Vector128<int> AsVector128Unsafe(this Int2 value)
        {
            Unsafe.SkipInit(out Vector128<int> result);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref result), value);
            return result;
        }

        public static Int2 AsInt2(this Vector128<int> value)
        {
            ref byte address = ref Unsafe.As<Vector128<int>, byte>(ref value);
            return Unsafe.ReadUnaligned<Int2>(ref address);
        }

        public static Vector2 AsVector2(this Int2 value) => new Vector2(value.X, value.Y);
    }
}
