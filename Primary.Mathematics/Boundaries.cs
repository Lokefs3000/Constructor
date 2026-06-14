using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Primary.Mathematics
{
    public struct Boundaries : IEquatable<Boundaries>, IFormattable
    {
        public Vector2 Minimum;
        public Vector2 Maximum;

        public Boundaries()
        {
            Minimum = Vector2.Zero;
            Maximum = Vector2.Zero;
        }

        public Boundaries(Vector2 minimum, Vector2 maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        public Boundaries(Vector4 minMax)
        {
            this = Unsafe.ReadUnaligned<Boundaries>(ref Unsafe.As<Vector4, byte>(ref minMax));
        }

        public Boundaries(Vector128<float> vector)
        {
            this = Unsafe.ReadUnaligned<Boundaries>(ref Unsafe.As<Vector128<float>, byte>(ref vector));
        }

        public bool IsWithin(Vector2 point)
        {
            return Vector128.GreaterThanOrEqualAll(Vector128.Create(point.X, point.Y, Maximum.X, Maximum.Y), Vector128.Create(Minimum.X, Minimum.Y, point.X, point.Y));
        }

        public bool IsIntersecting(Boundaries boundaries)
        {
            Vector128<float> a = AsVector128();
            if (Sse.IsSupported) //TODO: VALIDATE IF THIS IS INLINED CORRECTLY
                a = Sse.Shuffle(a, a, s_intersectShuffle);
            else
                throw new NotImplementedException(); //TODO: add fallback for no SIMD support

            Vector128<float> b = -boundaries.AsVector128();
            return Vector128.LessThanOrEqualAll(b, a);
        }

        public bool Equals(Boundaries other)
        {
            return Vector128.EqualsAll(AsVector128(), other.AsVector128());
        }

        public override int GetHashCode()
        {
            return Minimum.GetHashCode() ^ Maximum.GetHashCode();
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        {
            return $"{{Minimum:{Minimum.ToString(format, formatProvider)} Maximum:{Maximum.ToString(format, formatProvider)}}}";
        }

        public override string ToString() => ToString("G", CultureInfo.CurrentCulture);
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);

        public Vector4 AsVector4() => new Vector4(Minimum.X, Minimum.Y, Maximum.X, Maximum.Y);
        public Vector128<float> AsVector128() => Vector128.Create(Minimum.X, Minimum.Y, Maximum.X, Maximum.Y);

        public static Boundaries Grow(Boundaries boundaries, Vector2 amount)
        {
            Vector128<float> growVector = new Boundaries(-amount, amount).AsVector128();
            Vector128<float> minMaxVector = boundaries.AsVector128();

            return new Boundaries(minMaxVector + growVector);
        }

        public static Boundaries Offset(Boundaries boundaries, Vector2 offset)
        {
            return new Boundaries(Vector128.Add(boundaries.AsVector128(), Vector128.Create(offset.X, offset.Y, offset.X, offset.Y)));
        }

        public static Boundaries Union(Boundaries a, Boundaries b)
        {
            Vector128<float> aVector = new Boundaries(a.Minimum, -a.Maximum).AsVector128();
            Vector128<float> bVector = new Boundaries(b.Minimum, -b.Maximum).AsVector128();

            Boundaries bounds = new Boundaries(Vector128.Min(aVector, bVector));
            bounds.Maximum = -bounds.Maximum;

            return bounds;
        }

        public static Vector2 OnEdge(Boundaries b, Vector2 p)
        {
            return Vector2.Clamp(p, b.Minimum, b.Maximum);
        }

        public static Boundaries Clip(Boundaries a, Boundaries b)
        {
            //TODO: Vectorize to use Vector128 instead of 2 Vector2s
            return new Boundaries(Vector2.Max(a.Minimum, b.Minimum), Vector2.Min(a.Maximum, b.Maximum));
        }

        public Vector2 Size => Maximum - Minimum;
        public Vector2 Center => Vector2.Lerp(Minimum, Maximum, 0.5f);

        public static Boundaries operator +(Boundaries left, Vector2 right) => Offset(left, right);
        public static Boundaries operator *(Boundaries left, float right) => new Boundaries(left.AsVector128() * right);

        public static readonly Boundaries Zero = new Boundaries();

        private const byte s_intersectShuffle = (byte)((1 << 6) | (0 << 4) | (3 << 2) | 2);

        public static bool operator ==(Boundaries a, Boundaries b) => a.Equals(b);
        public static bool operator !=(Boundaries a, Boundaries b) => !a.Equals(b);
    }

    public static partial class Extensions
    {
        extension(Rect rect)
        {
            public Boundaries AsBoundaries()
            {
                return new Boundaries(rect.Position.AsVector2(), rect.Maximum.AsVector2());
            }
        }
    }
}
