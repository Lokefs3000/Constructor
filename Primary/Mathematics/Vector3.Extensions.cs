using System.Numerics;
using System.Runtime.Intrinsics;

namespace Primary.Mathematics
{
    public static class Vector3Ext
    {
        public static Vector3 Floor(Vector3 vector)
        {
            return Vector128.Floor(vector.AsVector128Unsafe()).AsVector3();
        }

        public static Vector3 Ceiling(Vector3 vector)
        {
            return Vector128.Ceiling(vector.AsVector128Unsafe()).AsVector3();
        }
    }
}
