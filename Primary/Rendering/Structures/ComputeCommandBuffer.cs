using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI2;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Structures
{
    public ref struct ComputeCommandBuffer
    {
        private readonly CSCommandBuffer _commandBuffer;

        internal ComputeCommandBuffer(CSCommandBuffer commandBuffer)
        {
            _commandBuffer = commandBuffer;
        }

        public void SetPipeline(RHIComputePipeline pipeline) => _commandBuffer.SetPipeline(pipeline);

        public void SetProperties(PropertyBlock block) => _commandBuffer.SetProperties(block);
        public void SetProperties(ROPropertyBlock block) => _commandBuffer.SetProperties(block);

        public void SetConstants<T>(T data) where T : unmanaged => _commandBuffer.SetConstants(data);
        public void SetConstants(ReadOnlySpan<uint> data) => _commandBuffer.SetConstants(data);

        public void Dispatch(uint threadGroupX, uint threadGroupY, uint threadGroupZ) => _commandBuffer.Dispatch(threadGroupX, threadGroupY, threadGroupZ);

        public void Upload<T>(FGBufferUploadDesc desc, ReadOnlySpan<T> data) where T : unmanaged => _commandBuffer.Upload(desc, data);
        public void Upload<T>(FrameGraphBuffer buffer, T data) where T : unmanaged => _commandBuffer.Upload(buffer, data);

        public void Upload<T>(FGTextureUploadDesc desc, Span<T> data) where T : unmanaged => _commandBuffer.Upload(desc, data);

        public void Copy(FGBufferCopyDesc desc) => _commandBuffer.Copy(desc);
        public void Copy(FGTextureCopyDesc desc) => _commandBuffer.Copy(desc);

        public FGMappedSubresource<T> Map<T>(FGMapBufferDesc desc) where T : unmanaged => _commandBuffer.Map<T>(desc);
        public FGMappedSubresource<T> Map<T>(FrameGraphTexture texture) where T : unmanaged => _commandBuffer.Map<T>(texture);
    }
}
