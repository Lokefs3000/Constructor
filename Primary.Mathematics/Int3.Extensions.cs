using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Primary.Mathematics
{
    public static partial class Extensions
    {
        public static Vector128<int> AsVector128Unsafe(this Int3 value)
        {
            Unsafe.SkipInit(out Vector128<int> result);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref result), value);
            return result;
        }

        public static Vector128<int> AsVector128(this Int3 value)
        {
            Vector128<int> result = Vector128<int>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref result), value);
            return result;
        }

        public static Vector128<int> AsVector128(this Int3 value, int defaultValue)
        {
            Vector128<int> result = Vector128.Create(defaultValue);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref result), value);
            return result;
        }

        public static Int3 AsInt3(this Vector128<int> value)
        {
            ref byte address = ref Unsafe.As<Vector128<int>, byte>(ref value);
            return Unsafe.ReadUnaligned<Int3>(ref address);
        }

        public static Vector3 AsVector2(this Int3 value) => new Vector3(value.X, value.Y, value.Z);

        public static Int3 AsInt3(this Vector3 value) => new Int3((int)value.X, (int)value.Y, (int)value.Z);
    }
}
