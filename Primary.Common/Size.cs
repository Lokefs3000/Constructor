using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Primary.Common
{
    public record struct Size : IEquatable<Size>, IFormattable
    {
        public int Width;
        public int Height;

        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return ToString("G", CultureInfo.CurrentCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return $"{Width.ToString(format, formatProvider)} x {Height.ToString(format, formatProvider)}";
        }

        public bool Equals(Size? other) => other is not null && other.Value.Width == Width && other.Value.Height == Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Extend(Size size)
        {
            Width += size.Width;
            Height += size.Height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 AsVector2() => new Vector2(Width, Height);

        public float Aspect => Width / (float)Height;
    }
}
