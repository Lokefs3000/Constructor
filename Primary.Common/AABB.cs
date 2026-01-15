using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.Intrinsics;

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

        public AABB(Vector128<float> minimum, Vector128<float> maximum)
        {
            Minimum = minimum.AsVector3();
            Maximum = maximum.AsVector3();
        }

        public AABB(Vector256<float> value)
        {
            Minimum = value.GetLower().AsVector3();
            Maximum = value.GetUpper().AsVector3();
        }

        public Vector256<float> AsVector256() => Vector256.Create(Minimum.AsVector128(), Maximum.AsVector128());
        public Vector256<float> AsVector256Unsafe() => Vector256.Create(Minimum.AsVector128Unsafe(), Maximum.AsVector128Unsafe());

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

        public static AABB Offset(AABB aabb, Vector3 offset)
        {
            return new AABB(aabb.Minimum + offset, aabb.Maximum + offset);
        }

        public static AABB Add(AABB left, AABB right)
        {
            return new AABB(Vector256.Add(left.AsVector256Unsafe(), right.AsVector256Unsafe()));
        }

        public static AABB Subtract(AABB left, AABB right)
        {
            return new AABB(Vector256.Subtract(left.AsVector256Unsafe(), right.AsVector256Unsafe()));
        }

        public static AABB FromExtents(Vector3 center, Vector3 extents)
        {
            Vector3 half = extents * 0.5f;
            return new AABB(center - half, center + half);
        }

        public static readonly AABB Zero = new AABB();
        public static readonly AABB Infinite = new AABB(Vector3.NegativeInfinity, Vector3.PositiveInfinity);

        public static AABB operator +(AABB left, AABB right) => Add(left, right);
        public static AABB operator -(AABB left, AABB right) => Subtract(left, right);
    }
}
