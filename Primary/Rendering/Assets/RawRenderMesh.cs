using Primary.Common;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Assets
{
    public abstract class RawRenderMesh
    {
        protected readonly IRenderMeshSource _source;
        protected int _uniqueId;

        protected AABB _boundaries;

        protected uint _vertexOffset;
        protected uint _indexOffset;
        protected uint _indexCount;

        protected bool _hasIndices;

        public RawRenderMesh(IRenderMeshSource source, int uniqueId, AABB boundaries, uint vertexOffset, uint indexOffset, uint indexCount, bool hasIndices)
        {
            _source = source;
            _uniqueId = uniqueId;
            _boundaries = boundaries;
            _vertexOffset = vertexOffset;
            _indexOffset = indexOffset;
            _indexCount = indexCount;
            _hasIndices = hasIndices;
        }

        public RawRenderMesh(IRenderMeshSource source)
        {
            _source = source;
            _uniqueId = 0;
            _boundaries = AABB.Zero;
            _vertexOffset = 0;
            _indexOffset = 0;
            _indexCount = 0;
            _hasIndices = false;
        }

        public IRenderMeshSource Source => _source;
        public int UniqueId => _uniqueId;

        public AABB Boundaries => _boundaries;

        public uint VertexOffset => _vertexOffset;
        public uint IndexOffset => _indexOffset;
        public uint IndexCount => _indexCount;

        public bool HasIndices => _hasIndices;
    }

    public interface IRawRenderMesh
    {
        public IRenderMeshSource? MeshSource { get; }
        public AABB Boundaries { get; }
        public ref readonly RenderMeshDrawArgs Args { get; }

        public int UniqueId { get; }
        public int LoadIndex { get; }
    }

    public readonly record struct RenderMeshDrawArgs(IRenderMeshSource MeshSource, uint VertexOffset, uint IndexOffset, uint VertexOrIndexCount, bool NeedsIndexedDraw);
}
