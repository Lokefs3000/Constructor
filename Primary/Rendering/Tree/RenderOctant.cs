using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Primary.Mathematics;

namespace Primary.Rendering.Tree
{
    public sealed class RenderOctant
    {
        private readonly RenderOctree _octree;
        private readonly RenderExtents _sourceExtents;

        private readonly RenderOctant? _parent;
        private readonly int _treeDepth;

        private readonly byte _entityIndex;
        private readonly byte _entityBitShift;

        private RenderExtents _currentExtents;

        private RenderOctant[]? _octants;
        private int _childrenEntityCount;
        private int _entityCount;

        internal RenderOctant(RenderOctree octree, RenderExtents sourceExtents, RenderOctant? parent, byte entityIndex, byte entityBitShift)
        {
            _octree = octree;
            _sourceExtents = sourceExtents;

            _parent = parent;
            _treeDepth = (parent?._treeDepth ?? 0) + 1;

            _entityIndex = entityIndex;
            _entityBitShift = entityBitShift;

            _currentExtents = sourceExtents;

            _octants = null;
            _childrenEntityCount = 0;
            _entityCount = 0;
        }

        internal bool DoRenderExtentsFit(RenderExtents extents)
        {
            float excessSpace = RenderOctree.CalculateExcessSpace(_sourceExtents, extents);
            return excessSpace < RenderOctree.MaxOctantLoseness;
        }

        internal bool DoRenderExtentsFit(RenderExtents extents, out float excessSpace)
        {
            excessSpace = RenderOctree.CalculateExcessSpace(_sourceExtents, extents);
            return excessSpace < RenderOctree.MaxOctantLoseness;
        }

        internal int IncrementCount()
        {
            int count = Interlocked.Increment(ref _entityCount);

            if (_parent == null)
                count += _octree.IncrementChildCount();
            else
                count += Interlocked.Increment(ref _parent._childrenEntityCount);

            return count;
        }

        internal int DecrementCount()
        {
            int count = Interlocked.Decrement(ref _entityCount);

            if (_parent == null)
                count += _octree.DecrementChildCount();
            else
                count += Interlocked.Decrement(ref _parent._childrenEntityCount);

            return count;
        }

        internal int IncrementCountRaw() => ++_entityCount;
        internal int DecrementCountRaw() => --_entityCount;

        public RenderExtents SourceExtents => _sourceExtents;

        public RenderOctant? Parent => _parent;
        public int TreeDepth => _treeDepth;

        public byte EntityIndex => _entityIndex;
        public byte EntityBitShift => _entityBitShift;

        public RenderExtents CurrentExtents { get => _currentExtents; internal set => _currentExtents = value; }

        public RenderOctant[]? Octants { get => _octants; internal set => _octants = value; }
        public int ChildrenEntityCount { get => _childrenEntityCount; internal set => _childrenEntityCount = value; }
        public int EntityCount { get => _entityCount; internal set => _entityCount = value; }

        [MemberNotNullWhen(false, nameof(_octants), nameof(Octants))]
        public bool IsLeaf => _octants == null;
    }
}
