using System;
using System.Collections.Generic;
using System.Text;
using Primary.Rendering.Data;
using Primary.Rendering.Passes;

namespace Primary.Rendering
{
    public readonly record struct FrameGraphBuilder
    {
        private readonly FrameGraph _frameGraph;
        private readonly IRenderPass _renderPass;
        private readonly RenderContextContainer _contextContainer;
        private readonly RenderPassBlackboard _blackboard;

        internal FrameGraphBuilder(FrameGraph frameGraph, IRenderPass renderPass, RenderContextContainer contextContainer, RenderPassBlackboard blackboard)
        {
            _frameGraph = frameGraph;
            _renderPass = renderPass;
            _contextContainer = contextContainer;
            _blackboard = blackboard;
        }

        public RasterPassBuilder AddRasterPass(string passName)
        {
            RenderPassIndex index = _frameGraph.RegisterNewPass(_renderPass, RenderPassType.Raster, passName, _contextContainer);
            return new RasterPassBuilder(_frameGraph, index);
        }

        public RasterPassBuilder AddRasterPass<T>(string passName, out T passData) where T : class, IPassData, new()
        {
            RenderPassIndex index = _frameGraph.RegisterNewPass(_renderPass, RenderPassType.Raster, passName, _contextContainer);

            passData = _frameGraph.GetBlankPassData<T>();
            index.PassData = passData;

            return new RasterPassBuilder(_frameGraph, index);
        }

        public ComputePassBuilder AddComputePass(string passName)
        {
            RenderPassIndex index = _frameGraph.RegisterNewPass(_renderPass, RenderPassType.Compute, passName, _contextContainer);
            return new ComputePassBuilder(_frameGraph, index);
        }

        public ComputePassBuilder AddComputePass<T>(string passName, out T passData) where T : class, IPassData, new()
        {
            RenderPassIndex index = _frameGraph.RegisterNewPass(_renderPass, RenderPassType.Compute, passName, _contextContainer);

            passData = _frameGraph.GetBlankPassData<T>();
            index.PassData = passData;

            return new ComputePassBuilder(_frameGraph, index);
        }

        public T GetContext<T>() where T : class, IContextItem => _contextContainer.Get<T>();
        public bool HasContext<T>() where T : class, IContextItem => _contextContainer.Has<T>();

        public T AddBlackboard<T>() where T : class, IBlackboardData, new() => _blackboard.Add<T>();
        public T? GetBlackboard<T>() where T : class, IBlackboardData, new() => _blackboard.Get<T>();
    }
}
