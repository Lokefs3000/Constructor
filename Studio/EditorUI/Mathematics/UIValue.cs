using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace EditorUI.Mathematics
{
    public struct UIValue : IEquatable<UIValue>, IFormattable
    {
        public float Relative;
        public int Absolute;

        public UIValue()
        {
            Relative = 0.0f;
            Absolute = 0;
        }

        public UIValue(float relative, int absolute)
        {
            Relative = relative;
            Absolute = absolute;
        }

        public UIValue(float relative)
        {
            Relative = relative;
            Absolute = 0;
        }

        public UIValue(int absolute)
        {
            Relative = 0.0f;
            Absolute = absolute;
        }

        public readonly float Evaluate(float space)
        {
            return Relative == 0.0f ? Absolute : (Relative * space + Absolute);
        }

        public readonly override bool Equals([NotNullWhen(true)] object? obj) => obj is UIValue value && Equals(value);
        public readonly bool Equals(UIValue other) => Relative == other.Relative && Absolute == other.Absolute;

        public readonly override int GetHashCode() => HashCode.Combine(Relative, Absolute);

        public readonly override string ToString() => ToString(null, CultureInfo.CurrentCulture);
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);
        public readonly string ToString(IFormatProvider? formatProvider) => ToString(null, CultureInfo.CurrentCulture);
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        {
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            return $"{{ {Relative.ToString(format, formatProvider)}{separator} {Absolute.ToString(format, formatProvider)} }}";
        }

        public static bool operator ==(UIValue left, UIValue right) => left.Equals(right);
        public static bool operator !=(UIValue left, UIValue right) => !(left == right);

        public static readonly UIValue Zero = new UIValue();
        public static readonly UIValue Max = new UIValue(1.0f);
    }
}
