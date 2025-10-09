using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering.Data
{
    public abstract class RawRenderMesh
    {
        private IRenderMeshSource _source;
        private GCHandle _handle;

        private readonly uint _vertexOffset;
        private readonly uint _indexOffset;

        private readonly uint _indexCount;

        protected RawRenderMesh(IRenderMeshSource source, uint vertexOffset, uint indexOffset, uint indexCount)
        {
            _source = source;
            _handle = GCHandle.Alloc(this, GCHandleType.Normal);

            _vertexOffset = vertexOffset;
            _indexOffset = indexOffset;

            _indexCount = indexCount;
        }

        protected void FreeHandle()
        {
            _handle.Free();
        }

        public IRenderMeshSource Source => _source;
        public nint Handle => GCHandle.ToIntPtr(_handle);

        public uint VertexOffset => _vertexOffset;
        public uint IndexOffset => _indexOffset;

        public uint IndexCount => _indexCount;
    }
}
