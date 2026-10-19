using Primary.Rendering.Datatypes;
using Primary.Rendering.Passes;
using Primary.RHI;

namespace Primary.Rendering
{
    public readonly struct RasterPassBuilder : IDisposable
    {
        private readonly FrameGraph _frameGraph;
        private readonly RenderPassIndex _renderPass;

        internal RasterPassBuilder(FrameGraph frameGraph, RenderPassIndex renderPass)
        {
            _frameGraph = frameGraph;
            _renderPass = renderPass;
        }

        public void Dispose() => _frameGraph.RenderPassManager.FinishPassBuild(_renderPass);

        public readonly void SetFunction(Action<RasterPassContext> context) => _renderPass.RasterEntrypoint = context;

        #region Transient
        public readonly BufferHandle CreateBuffer(in FGBufferDesc bufferDesc) => _frameGraph.ResourceManager.CreateBuffer(in bufferDesc);
        public readonly TextureHandle CreateTexture(in FGTextureDesc textureDesc) => _frameGraph.ResourceManager.CreateTexture(in textureDesc);
        #endregion

        #region External
        public readonly BufferHandle ImportBuffer(in RHIBuffer buffer) => _frameGraph.ResourceManager.ImportBuffer(in buffer);
        public readonly TextureHandle ImportTexture(in RHITexture texture) => _frameGraph.ResourceManager.ImportTexture(in texture);
        #endregion

        #region Readback
        public readonly ReadbackRequest RequestBufferReadback(BufferHandle bufferHandle, FGRange? range = null)
        {
            throw new NotImplementedException();
        }

        public readonly ReadbackRequest RequestTextureReadback(TextureHandle textureHandle, FGBox? rect = null, uint slice = 0)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Usage
        public readonly void Write(ResourceHandle resource)
        {
            _frameGraph.ResourceManager.AddWrite(_renderPass, resource);
        }

        public readonly void Read(ResourceHandle resource)
        {
            _frameGraph.ResourceManager.AddRead(_renderPass, resource);
        }

        public readonly void ReadWrite(ResourceHandle resource)
        {
            _frameGraph.ResourceManager.AddReadWrite(_renderPass, resource);
        }
        #endregion
    }
}
