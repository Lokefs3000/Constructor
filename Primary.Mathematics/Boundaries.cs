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
            Vector128<float> a = Vector128.Xor(AsVector128(), Vector128.Create(0.0f, 0.0f, -0.0f, -0.0f));
            Vector128<float> b = Vector128.Xor(boundaries.AsVector128(), Vector128.Create(-0.0f, -0.0f, 0.0f, 0.0f));

            b = Vector128.Shuffle(b, Vector128.Create(2, 3, 0, 1));

            return Vector128.LessThanAll(a, b);
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

        public static Boundaries Grow(Boundaries boundaries, Vector4 amount)
        {
            Vector128<float> growVector = new Boundaries(new Vector4(-amount.X, -amount.Y, amount.Z, amount.W)).AsVector128();
            Vector128<float> minMaxVector = boundaries.AsVector128();

            return new Boundaries(minMaxVector + growVector);
        }

        public static Boundaries Offset(Boundaries boundaries, Vector2 offset)
        {
            return new Boundaries(Vector128.Add(boundaries.AsVector128(), Vector128.Create(offset.X, offset.Y, offset.X, offset.Y)));
        }

        public static Boundaries Union(Boundaries a, Boundaries b)
        {
            Vector128<float> aVector = a.AsVector128() * Vector128.Create(1.0f, 1.0f, -1.0f, -1.0f); ;
            Vector128<float> bVector = b.AsVector128() * Vector128.Create(1.0f, 1.0f, -1.0f, -1.0f); ;

            return Unsafe.BitCast<Vector128<float>, Boundaries>(Vector128.Min(aVector, bVector) * Vector128.Create(1.0f, 1.0f, -1.0f, -1.0f));
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

        public static Vector2 Contain(Boundaries boundaries, Vector2 position)
        {
            return Vector2.Min(Vector2.Max(position, boundaries.Minimum), boundaries.Maximum);
        }

        public Vector2 Size => Maximum - Minimum;
        public Vector2 Center => Vector2.Lerp(Minimum, Maximum, 0.5f);

        public static Boundaries operator +(Boundaries left, Vector2 right) => Offset(left, right);
        public static Boundaries operator *(Boundaries left, float right) => new Boundaries(left.AsVector128() * right);

        public static readonly Boundaries Zero = new Boundaries();
        public static readonly Boundaries One = new Boundaries(Vector2.Zero, Vector2.One);

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
