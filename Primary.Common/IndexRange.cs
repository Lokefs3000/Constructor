using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

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

        public bool Equals(IndexRange other) => Start == other.Start && End == other.End;
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is IndexRange other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Start, End);
        public override string ToString() => $"{Start} - {End}";

        public int Length => End - Start;

        public static implicit operator Range(IndexRange range) => new Range(range.Start, range.End);
    }
}
