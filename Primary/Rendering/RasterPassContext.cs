using System;
using System.Collections.Generic;
using System.Text;
using Primary.Rendering.Data;

namespace Primary.Rendering
{
    public readonly record struct RasterPassContext
    {
        private readonly FrameGraph _frameGraph;

        private readonly RasterCommandBuffer _commandBuffer;
        private readonly IPassData? _passData;
        private readonly RenderContextContainer _contextContainer;
        private readonly RenderPassBlackboard _renderPassBlackboard;

        internal RasterPassContext(FrameGraph frameGraph, RasterCommandBuffer commandBuffer, IPassData? passData, RenderContextContainer contextContainer, RenderPassBlackboard renderPassBlackboard)
        {
            _frameGraph = frameGraph;

            _commandBuffer = commandBuffer;
            _passData = passData;
            _contextContainer = contextContainer;
            _renderPassBlackboard = renderPassBlackboard;
        }

        public T? GetPassData<T>() where T : class, IPassData, new() => (T?)_passData;
        public T GetContext<T>() where T : class, IContextItem => _contextContainer.Get<T>();
        public T GetBlackboard<T>() where T : class, IBlackboardData, new() => _renderPassBlackboard.Get<T>()!;

        public ref readonly FGBufferDesc GetDescription(in BufferHandle handle) => ref _frameGraph.ResourceManager.GetDescription(in handle);
        public ref readonly FGTextureDesc GetDescription(in TextureHandle handle) => ref _frameGraph.ResourceManager.GetDescription(in handle);

        public RasterCommandBuffer CommandBuffer => _commandBuffer;
    }
}
