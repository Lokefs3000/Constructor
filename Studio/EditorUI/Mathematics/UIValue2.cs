using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace EditorUI.Mathematics
{
    public struct UIValue2 : IEquatable<UIValue2>, IFormattable
    {
        public UIValue X;
        public UIValue Y;

        public UIValue2()
        {
            X = UIValue.Zero;
            Y = UIValue.Zero;
        }

        public UIValue2(UIValue x, UIValue y)
        {
            X = x;
            Y = y;
        }

        public UIValue2(UIValue scalar)
        {
            X = scalar;
            Y = scalar;
        }

        public UIValue2(float relativeX, int absoluteX, float relativeY, int absoluteY)
        {
            X = new UIValue(relativeX, absoluteX);
            Y = new UIValue(relativeY, absoluteY);
        }

        public UIValue2(float relativeX, float relativeY)
        {
            X = new UIValue(relativeX);
            Y = new UIValue(relativeY);
        }

        public UIValue2(int absoluteX, int absoluteY)
        {
            X = new UIValue(absoluteX);
            Y = new UIValue(absoluteY);
        }

        public readonly Vector2 Evaluate(Vector2 space)
        {
            return new Vector2(X.Evaluate(space.X), Y.Evaluate(space.Y));
        }

        public readonly override bool Equals([NotNullWhen(true)] object? obj) => obj is UIValue2 value && Equals(value);
        public readonly bool Equals(UIValue2 other) => X.Equals(other.X) && Y.Equals(other.Y);

        public readonly override int GetHashCode() => HashCode.Combine(X, Y);

        public readonly override string ToString() => ToString(null, CultureInfo.CurrentCulture);
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);
        public readonly string ToString(IFormatProvider? formatProvider) => ToString(null, CultureInfo.CurrentCulture);
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        {
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            return $"{{ X:{X.ToString(format, formatProvider)}{separator} Y:{Y.ToString(format, formatProvider)} }}";
        }

        public static bool operator ==(UIValue2 left, UIValue2 right) => left.Equals(right);
        public static bool operator !=(UIValue2 left, UIValue2 right) => !(left == right);

        public static readonly UIValue2 Zero = new UIValue2();
        public static readonly UIValue2 MaxX = new UIValue2(1.0f, 0.0f);
        public static readonly UIValue2 MaxY = new UIValue2(0.0f, 1.0f);
        public static readonly UIValue2 Max = new UIValue2(1.0f, 1.0f);
    }
}
