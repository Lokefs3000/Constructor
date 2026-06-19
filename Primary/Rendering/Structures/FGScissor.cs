using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Primary.Mathematics;

namespace Primary.Rendering.Structures
{
    public record struct FGRect(int Left, int Top, int Right, int Bottom) : IEquatable<FGRect>
    {
        public int Width { get => Right - Left; set => Right = Left + value; }
        public int Height { get => Bottom - Top; set => Bottom = Top + value; }

        public FGRect(Rect rect) : this(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height)
        {
        }

        public bool Equals(FGRect other)
        {
            return Vector128.EqualsAll(AsVector128(), other.AsVector128());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Left, Top, Right, Bottom);
        }

        private Vector128<int> AsVector128()
        {
            return Unsafe.ReadUnaligned<Vector128<int>>(ref Unsafe.As<FGRect, byte>(ref this));
        }

        public static bool Intersects(FGRect a, FGRect b)
        {
            return !Vector128.LessThanAny(
                Vector128.Create(a.Left, b.Left, b.Bottom, a.Bottom),
                Vector128.Create(b.Right, a.Right, a.Top, b.Top));
        }
    }
}
