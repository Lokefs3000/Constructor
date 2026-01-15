using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Primary.Rendering.Structures
{
    public record struct FGRect(int Left, int Top, int Right, int Bottom) : IEquatable<FGRect>
    {
        public int Width { get => Right - Left; set => Right = Left + value; }
        public int Height { get => Bottom - Top; set => Bottom = Top + value; }

        public bool Equals(FGRect other)
        {
            return Vector128.EqualsAll(AsVector128(ref this), AsVector128(ref other));
        }

        private static Vector128<int> AsVector128(ref FGRect rect)
        {
            Vector128<int> vector;
            Unsafe.SkipInit(out vector);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref vector), rect);
            return vector;
        }

        public static bool Intersects(FGRect a, FGRect b)
        {
            return !Vector128.LessThanAny(
                Vector128.Create(a.Left, b.Left, b.Bottom, a.Bottom),
                Vector128.Create(b.Right, a.Right, a.Top, b.Top));
        }
    }
}
