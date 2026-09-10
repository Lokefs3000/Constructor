using System.Diagnostics.CodeAnalysis;

namespace Primary.Common
{
    public struct IndexRange : IEquatable<IndexRange>
    {
        public int Start;
        public int End;

        public IndexRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public IndexRange(int length)
        {
            Start = 0;
            End = length;
        }

        public readonly bool Equals(IndexRange other) => Start == other.Start && End == other.End;
        public readonly override bool Equals([NotNullWhen(true)] object? obj) => obj is IndexRange other && Equals(other);

        public override readonly int GetHashCode() => HashCode.Combine(Start, End);
        public override readonly string ToString() => $"{Start}..{End}";

        public readonly bool IsWithinRange(int index) => index >= Start && index < End;
        public readonly bool IsWithinFullRange(int index) => index >= Start && index <= End;

        public readonly bool IsOverlappingRange(IndexRange range) => Start < range.End && End > range.Start;
        public readonly bool IsOverlappingFullRange(IndexRange range) => Start <= range.End && End >= range.Start;

        public readonly int Length => End - Start;
        public readonly bool IsEmpty => End <= Start;

        public static readonly IndexRange Empty = new IndexRange(0, 0);

        public static implicit operator Range(IndexRange range) => new Range(range.Start, range.End);
    }
}
