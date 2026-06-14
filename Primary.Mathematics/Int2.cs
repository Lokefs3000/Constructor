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
    public struct Int2 : IEquatable<Int2>, IFormattable
    {
        internal const int Alignment = 8;

        public int X;
        public int Y;

        internal const int ElementCount = 2;

        public Int2(int value)
        {
            this = Create(value);
        }

        public Int2(int x, int y)
        {
            this = Create(x, y);
        }

        public Int2(ReadOnlySpan<int> values)
        {
            this = Create(values);
        }

        public readonly override bool Equals([NotNullWhen(true)] object? obj) => obj is Vector2 other && Equals(other);
        public readonly bool Equals(Int2 other) => Unsafe.BitCast<Int2, ulong>(this) == Unsafe.BitCast<Int2, ulong>(other);

        public readonly override int GetHashCode() => HashCode.Combine(X, Y);

        public readonly override string ToString() => ToString("G", CultureInfo.CurrentCulture);
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}>";
        }

        public static readonly Int2 AllBitsSet = Vector128<int>.AllBitsSet.AsInt2();
        public static readonly Int2 MinValue = Create(int.MinValue);
        public static readonly Int2 MaxValue = Create(int.MaxValue);
        public static readonly Int2 One = Create(1);
        public static readonly Int2 UnitX = Create(1, 0);
        public static readonly Int2 UnitY = Create(0, 1);

        public static readonly Int2 Zero = default;

        public int this[int index]
        {
            readonly get => this.AsVector128Unsafe().GetElement(index);
            set => this = this.AsVector128Unsafe().WithElement(index, value).AsInt2();
        }

        public static Int2 Create(int x, int y) => Vector128.Create(x, y, 0, 0).AsInt2();
        public static Int2 Create(int value) => Vector128.Create(value).AsInt2();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int2 Create(ReadOnlySpan<int> values)
        {
            if (values.Length < ElementCount)
                throw new ArgumentOutOfRangeException(nameof(values));
            return Unsafe.ReadUnaligned<Int2>(ref Unsafe.As<int, byte>(ref MemoryMarshal.GetReference(values)));
        }

        public static Int2 CreateScalar(int x) => Vector128.CreateScalar(x).AsInt2();

        /// <summary><paramref name="left"/> > <paramref name="right"/></summary>
        public static bool GreaterThanAny(Int2 left, Int2 right) => Vector128.GreaterThanAny(left.AsVector128(), right.AsVector128());
        /// <summary><paramref name="left"/> >= <paramref name="right"/></summary>
        public static bool GreaterThanOrEqualAny(Int2 left, Int2 right) => left.X >= right.X || left.Y >= right.Y;
        /// <summary><paramref name="left"/> < <paramref name="right"/></summary>
        public static bool LessThanAny(Int2 left, Int2 right) => Vector128.LessThanAny(left.AsVector128(), right.AsVector128());
        /// <summary><paramref name="left"/> <= <paramref name="right"/></summary>
        public static bool LessThanOrEqualAny(Int2 left, Int2 right) => left.X <= right.X || left.Y <= right.Y;

        public static Int2 Min(Int2 a, Int2 b) => Vector128.Min(a.AsVector128Unsafe(), b.AsVector128Unsafe()).AsInt2();
        public static Int2 Max(Int2 a, Int2 b) => Vector128.Max(a.AsVector128Unsafe(), b.AsVector128Unsafe()).AsInt2();
        public static Int2 Clamp(Int2 a, Int2 min, Int2 max) => Vector128.Clamp(a.AsVector128Unsafe(), min.AsVector128Unsafe(), max.AsVector128Unsafe()).AsInt2();
        
        public static Int2 Abs(Int2 a) => Vector128.Abs(a.AsVector128Unsafe()).AsInt2();

        public static Int2 operator +(Int2 left, Int2 right) => (left.AsVector128Unsafe() + right.AsVector128Unsafe()).AsInt2();
        public static Int2 operator -(Int2 left, Int2 right) => (left.AsVector128Unsafe() - right.AsVector128Unsafe()).AsInt2();
        public static Int2 operator *(Int2 left, Int2 right) => (left.AsVector128Unsafe() * right.AsVector128Unsafe()).AsInt2();
        public static Int2 operator /(Int2 left, Int2 right) => (left.AsVector128Unsafe() / right.AsVector128Unsafe()).AsInt2();

        public static Int2 operator *(int left, Int2 right) => (left * right.AsVector128Unsafe()).AsInt2();
        public static Int2 operator *(Int2 left, int right) => (left.AsVector128Unsafe() * right).AsInt2();
        public static Int2 operator /(Int2 left, int right) => (left.AsVector128Unsafe() / right).AsInt2();

        public static Int2 operator &(Int2 left, Int2 right) => (left.AsVector128Unsafe() & right.AsVector128Unsafe()).AsInt2();
        public static Int2 operator |(Int2 left, Int2 right) => (left.AsVector128Unsafe() | right.AsVector128Unsafe()).AsInt2();
        public static Int2 operator ~(Int2 left) => (~left.AsVector128Unsafe()).AsInt2();
        public static Int2 operator >>(Int2 left, int shiftAmount) => (left.AsVector128Unsafe() >> shiftAmount).AsInt2();
        public static Int2 operator <<(Int2 left, int shiftAmount) => (left.AsVector128Unsafe() << shiftAmount).AsInt2();
        public static Int2 operator >>>(Int2 left, int shiftAmount) => (left.AsVector128Unsafe() >> shiftAmount).AsInt2();

        public static bool operator ==(Int2 left, Int2 right) => left.Equals(right);
        public static bool operator !=(Int2 left, Int2 right) => !left.Equals(right);

        public static bool operator >(Int2 left, Int2 right) => Vector128.GreaterThanAll(left.AsVector128(1), right.AsVector128());
        public static bool operator >=(Int2 left, Int2 right) => Vector128.GreaterThanOrEqualAll(left.AsVector128(1), right.AsVector128());
        public static bool operator <(Int2 left, Int2 right) => Vector128.LessThanAll(left.AsVector128(-1), right.AsVector128());
        public static bool operator <=(Int2 left, Int2 right) => Vector128.LessThanOrEqualAll(left.AsVector128(-1), right.AsVector128());
    }
}
