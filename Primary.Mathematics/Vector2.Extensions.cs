using System.Numerics;
using System.Runtime.Intrinsics;

namespace Primary.Mathematics
{
    public static partial class Extensions
    {
        public static Vector2 Floor(Vector2 vector)
        {
            return Vector128.Floor(vector.AsVector128Unsafe()).AsVector2();
        }

        public static Vector2 Ceiling(Vector2 vector)
        {
            return Vector128.Ceiling(vector.AsVector128Unsafe()).AsVector2();
        }
    }
}
