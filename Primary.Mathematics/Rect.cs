using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace Primary.Mathematics
{
    public record struct Rect : IEquatable<Rect>, IFormattable
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public Rect()
        {
            X = 0;
            Y = 0;
            Width = 0;
            Height = 0;
        }

        public Rect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rect(Int2 position, Int2 size)
        {
            X = position.X;
            Y = position.Y;
            Width = size.X;
            Height = size.Y;
        }

        public Rect(Int2 size)
        {
            X = 0;
            Y = 0;
            Width = size.X;
            Height = size.Y;
        }

        public readonly Int2 Maximum => new Int2(X + Width, Y + Height);

        public readonly bool Equals(Rect other) => this.AsVector128() == other.AsVector128();

        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public readonly override string ToString() => ToString("", CultureInfo.CurrentCulture);
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator} {Width.ToString(format, formatProvider)}{separator} {Height.ToString(format, formatProvider)}>";
        }

        public static Rect Inflate(Rect rect, int width, int height)
        {
            int w2 = width / 2;
            int h2 = height / 2;

            return (rect.AsVector128() + Vector128.Create(width - w2, height - h2, w2, h2)).AsRect();
        }

        public static Rect Expand(Rect rect, int width, int height)
        {
            return new Rect(rect.X, rect.Y, rect.Width + width, rect.Height + height);
        }

        public static Rect Shrink(Rect rect, int width, int height)
        {
            return new Rect(rect.X, rect.Y, rect.Width - width, rect.Height - height);
        }

        public static Rect Offset(Rect rect, int x, int y)
        {
            return new Rect(rect.X + x, rect.Y + y, rect.Width, rect.Height);
        }

        public static Rect OffsetMin(Rect rect, int x, int y)
        {
            return (rect.AsVector128() + Vector128.Create(x, y, -x, -y)).AsRect();
        }

        public static Int2 Contain(Rect rect, Int2 position)
        {
            return Int2.Clamp(position, rect.Position, rect.Maximum);
        }

        public static readonly Rect Zero = new Rect(0, 0, 0, 0);
        public static readonly Rect One = new Rect(0, 0, 1, 1);
    }

    public static partial class Extensions
    {
        extension(Rect rect)
        {
            public Vector128<int> AsVector128()
            {
                return Unsafe.ReadUnaligned<Vector128<int>>(ref Unsafe.As<Rect, byte>(ref rect));
            }

            public Int2 Position => new Int2(rect.X, rect.Y);
            public Int2 Size => new Int2(rect.Width, rect.Height);
        }

        extension(Vector128<int> vector)
        {
            public Rect AsRect()
            {
                return Unsafe.ReadUnaligned<Rect>(ref Unsafe.As<Vector128<int>, byte>(ref vector));
            }
        }

        extension(Boundaries boundaries)
        {
            public Rect AsRect()
            {
                Vector128<float> minmax = boundaries.AsVector128();
                Vector128<float> mask = Vector128.WithUpper(minmax, Vector64<float>.Zero);
                minmax -= Vector128.Shuffle(mask, Vector128.Create(2, 3, 0, 1));

                return Vector128.ConvertToInt32(minmax).AsRect();
            }
        }
    }
}
