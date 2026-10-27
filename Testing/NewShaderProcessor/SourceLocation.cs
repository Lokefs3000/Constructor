using System;
using System.Collections.Generic;
using System.Text;

namespace NewShaderProcessor
{
    public readonly record struct SourceLocation(string SourceName, int Line, int IndexInLine) : IEquatable<SourceLocation>
    {
        public override int GetHashCode() => HashCode.Combine(Line, IndexInLine);
        public override string ToString() => $"{SourceName} {Line}:{IndexInLine}";
    }
}
