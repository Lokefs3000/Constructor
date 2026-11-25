using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering
{
    public readonly record struct BufferHandle
    {
        private readonly ResourceHandle _handle;

        internal BufferHandle(ResourceHandle handle)
        {
            _handle = handle;
        }

        public readonly int Index => _handle.Index;
        public readonly bool IsImported => _handle.IsImported;

        internal readonly ushort LocalIndex => _handle.LocalIndex;

        public readonly bool IsNull => _handle.IsNull;

        public static readonly BufferHandle Null = new BufferHandle(ResourceHandle.Null);

        public static implicit operator ResourceHandle(BufferHandle handle) => handle._handle;
    }

    public record struct FGBufferDesc
    {
        public string? Name;

        public int ElementCount;
        public int Stride;

        public FGBufferTarget Target;

        public FGBufferDesc(int count, int stride, FGBufferTarget target, string? name = null)
        {
            ElementCount = count;
            Stride = stride;
            Target = target;
        }

        public FGBufferDesc(FGBufferDesc bufferDesc)
        {
            this = bufferDesc;
        }
    }

    public enum FGBufferTarget : byte
    {
        None = 0,

        VertexInput = 1 << 0,
        IndexInput = 1 << 1,
        
        Constant = 1 << 2,

        Structured = 1 << 3,
        Raw = 1 << 4,
        Append = 1 << 5,
        Counter = 1 << 6
    }
}
