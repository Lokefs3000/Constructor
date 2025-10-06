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

        public readonly override bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector2 other) && Equals(other);
        public readonly bool Equals(Int2 other) => this.AsVector128Unsafe() == other.AsVector128Unsafe();

        public readonly override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => ToString("G", CultureInfo.CurrentCulture);
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            return $"<{X.ToString(format, formatProvider)}{separator}, {Y.ToString(format, formatProvider)}>";
        }

        public static Int2 AllBitsSet => Vector128<int>.AllBitsSet.AsInt2();
        public static Int2 MinValue => Create(int.MinValue);
        public static Int2 MaxValue => Create(int.MaxValue);
        public static Int2 One => Create(1);
        public static Int2 UnitX => Create(1, 0);
        public static Int2 UnitY => Create(0, 1);

        public static Int2 Zero => default;

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
    }
}
