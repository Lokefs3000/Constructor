using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Primary.Mathematics
{
    //very influenced by:
    //https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Numerics/Vector2.cs
    public struct Int3 : IEquatable<Int3>, IFormattable
    {
        internal const int Alignment = 8;

        public int X;
        public int Y;
        public int Z;

        internal const int ElementCount = 3;

        public Int3(int value)
        {
            this = Create(value);
        }

        public Int3(int x, int y, int z)
        {
            this = Create(x, y, z);
        }

        public Int3(ReadOnlySpan<int> values)
        {
            this = Create(values);
        }

        public readonly override bool Equals([NotNullWhen(true)] object? obj) => obj is Vector2 other && Equals(other);
        public readonly bool Equals(Int3 other) => this.AsVector128() == other.AsVector128();

        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public readonly override string ToString() => ToString("G", CultureInfo.CurrentCulture);
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator} {Z.ToString(format, formatProvider)}>";
        }

        public static readonly Int3 AllBitsSet = Vector128<int>.AllBitsSet.AsInt3();
        public static readonly Int3 MinValue = Create(int.MinValue);
        public static readonly Int3 MaxValue = Create(int.MaxValue);
        public static readonly Int3 One = Create(1);
        public static readonly Int3 UnitX = Create(1, 0, 0);
        public static readonly Int3 UnitY = Create(0, 1, 0);
        public static readonly Int3 UnitZ = Create(0, 0, 1);

        public static readonly Int3 Zero = default;

        public int this[int index]
        {
            readonly get => this.AsVector128Unsafe().GetElement(index);
            set => this = this.AsVector128Unsafe().WithElement(index, value).AsInt3();
        }

        public static Int3 Create(int x, int y, int z) => Vector128.Create(x, y, z, 0).AsInt3();
        public static Int3 Create(int value) => Vector128.Create(value).AsInt3();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int3 Create(ReadOnlySpan<int> values)
        {
            if (values.Length < ElementCount)
                throw new ArgumentOutOfRangeException(nameof(values));
            return Unsafe.ReadUnaligned<Int3>(ref Unsafe.As<int, byte>(ref MemoryMarshal.GetReference(values)));
        }

        public static Int3 CreateScalar(int x) => Vector128.CreateScalar(x).AsInt3();

        /// <summary><paramref name="left"/> > <paramref name="right"/></summary>
        public static bool GreaterThanAny(Int3 left, Int3 right) => Vector128.GreaterThanAny(left.AsVector128(), right.AsVector128());
        /// <summary><paramref name="left"/> >= <paramref name="right"/></summary>
        public static bool GreaterThanOrEqualAny(Int3 left, Int3 right) => left.X >= right.X || left.Y >= right.Y;
        /// <summary><paramref name="left"/> < <paramref name="right"/></summary>
        public static bool LessThanAny(Int3 left, Int3 right) => Vector128.LessThanAny(left.AsVector128(), right.AsVector128());
        /// <summary><paramref name="left"/> <= <paramref name="right"/></summary>
        public static bool LessThanOrEqualAny(Int3 left, Int3 right) => left.X <= right.X || left.Y <= right.Y;

        public static Int3 Min(Int3 a, Int3 b) => Vector128.Min(a.AsVector128Unsafe(), b.AsVector128Unsafe()).AsInt3();
        public static Int3 Max(Int3 a, Int3 b) => Vector128.Max(a.AsVector128Unsafe(), b.AsVector128Unsafe()).AsInt3();
        public static Int3 Clamp(Int3 a, Int3 min, Int3 max) => Vector128.Clamp(a.AsVector128Unsafe(), min.AsVector128Unsafe(), max.AsVector128Unsafe()).AsInt3();
        
        public static Int3 Abs(Int3 a) => Vector128.Abs(a.AsVector128Unsafe()).AsInt3();

        public static Int3 operator +(Int3 left, Int3 right) => (left.AsVector128Unsafe() + right.AsVector128Unsafe()).AsInt3();
        public static Int3 operator -(Int3 left, Int3 right) => (left.AsVector128Unsafe() - right.AsVector128Unsafe()).AsInt3();
        public static Int3 operator *(Int3 left, Int3 right) => (left.AsVector128Unsafe() * right.AsVector128Unsafe()).AsInt3();
        public static Int3 operator /(Int3 left, Int3 right) => (left.AsVector128Unsafe() / right.AsVector128Unsafe()).AsInt3();

        public static Int3 operator *(int left, Int3 right) => (left * right.AsVector128Unsafe()).AsInt3();
        public static Int3 operator *(Int3 left, int right) => (left.AsVector128Unsafe() * right).AsInt3();
        public static Int3 operator /(Int3 left, int right) => (left.AsVector128Unsafe() / right).AsInt3();

        public static Int3 operator &(Int3 left, Int3 right) => (left.AsVector128Unsafe() & right.AsVector128Unsafe()).AsInt3();
        public static Int3 operator |(Int3 left, Int3 right) => (left.AsVector128Unsafe() | right.AsVector128Unsafe()).AsInt3();
        public static Int3 operator ~(Int3 left) => (~left.AsVector128Unsafe()).AsInt3();
        public static Int3 operator >>(Int3 left, int shiftAmount) => (left.AsVector128Unsafe() >> shiftAmount).AsInt3();
        public static Int3 operator <<(Int3 left, int shiftAmount) => (left.AsVector128Unsafe() << shiftAmount).AsInt3();
        public static Int3 operator >>>(Int3 left, int shiftAmount) => (left.AsVector128Unsafe() >> shiftAmount).AsInt3();

        public static bool operator ==(Int3 left, Int3 right) => left.Equals(right);
        public static bool operator !=(Int3 left, Int3 right) => !left.Equals(right);

        public static bool operator >(Int3 left, Int3 right) => Vector128.GreaterThanAll(left.AsVector128(1), right.AsVector128());
        public static bool operator >=(Int3 left, Int3 right) => Vector128.GreaterThanOrEqualAll(left.AsVector128(1), right.AsVector128());
        public static bool operator <(Int3 left, Int3 right) => Vector128.LessThanAll(left.AsVector128(-1), right.AsVector128());
        public static bool operator <=(Int3 left, Int3 right) => Vector128.LessThanOrEqualAll(left.AsVector128(-1), right.AsVector128());
    }
}
