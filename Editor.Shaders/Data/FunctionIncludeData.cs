using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Shaders.Data
{
    public readonly record struct FunctionIncludeData
    {
        private readonly ReferenceIndex[] _indices;

        private readonly IndexRange _functionRange;
        private readonly IndexRange _resourceRange;
        private readonly IndexRange _properyRange;

        internal FunctionIncludeData(ReferenceIndex[] indices, IndexRange functionRange, IndexRange resourceRange, IndexRange properyRange)
        {
            _indices = indices;

            _functionRange = functionRange;
            _resourceRange = resourceRange;
            _properyRange = properyRange;
        }

        public ReadOnlySpan<ReferenceIndex> Indices => _indices.AsSpan();

        public ReadOnlySpan<ReferenceIndex> UsedFunctionIndices => _indices.AsSpan(_functionRange.Start, _functionRange.Length);
        public ReadOnlySpan<ReferenceIndex> UsedResourceIndices => _indices.AsSpan(_resourceRange.Start, _resourceRange.Length);
        public ReadOnlySpan<ReferenceIndex> UsedPropertyIndices => _indices.AsSpan(_properyRange.Start, _properyRange.Length);

        public static readonly FunctionIncludeData Empty = new FunctionIncludeData(Array.Empty<ReferenceIndex>(), IndexRange.Empty, IndexRange.Empty, IndexRange.Empty);
    }
}
