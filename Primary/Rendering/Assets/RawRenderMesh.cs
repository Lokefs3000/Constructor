using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering.Assets
{
    public abstract class RawRenderMesh
    {
        private readonly IRenderMeshSource _source;
        private readonly int _uniqueId;

        private readonly AABB _boundaries;

        private readonly uint _vertexOffset;
        private readonly uint _indexOffset;
        private readonly uint _indexCount;

        public RawRenderMesh(IRenderMeshSource source, int uniqueId, AABB boundaries, uint vertexOffset, uint indexOffset, uint indexCount)
        {
            _source = source;
            _uniqueId = uniqueId;
            _boundaries = boundaries;
            _vertexOffset = vertexOffset;
            _indexOffset = indexOffset;
            _indexCount = indexCount;
        }

        public IRenderMeshSource Source => _source;
        public int UniqueId => _uniqueId;

        public AABB Boundaries => _boundaries;

        public uint VertexOffset => _vertexOffset;
        public uint IndexOffset => _indexOffset;
        public uint IndexCount => _indexCount;
    }
}
