using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Common
{
    public struct AABB : IEquatable<AABB>, IFormattable
    {
        public Vector3 Minimum;
        public Vector3 Maximum;

        public AABB()
        {
            Minimum = Vector3.Zero;
            Maximum = Vector3.Zero;
        }

        public AABB(Vector3 minimum, Vector3 maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        {
            return $"{Minimum.ToString(format, formatProvider)} : {Maximum.ToString(format, formatProvider)}";
        }

        public override string ToString() => ToString(null, null);
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, null);

        public bool Equals(AABB other)
        {
            return Minimum == other.Minimum && Maximum == other.Maximum;
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => obj is AABB aabb && Equals(aabb);

        public override int GetHashCode()
        {
            return HashCode.Combine(Minimum, Maximum);
        }

        public Vector3 Size => Maximum - Minimum;
        public Vector3 Center => Vector3.Lerp(Minimum, Maximum, 0.5f);

        public static readonly AABB Zero = new AABB();
        public static readonly AABB Infinite = new AABB(Vector3.NegativeInfinity, Vector3.PositiveInfinity);
    }
}
