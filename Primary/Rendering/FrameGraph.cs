using System.Runtime.CompilerServices;
using Primary.Collections.ReadOnly;
using Primary.Profiling;
using Primary.Rendering.Batching;
using Primary.Rendering.Configuration;
using Primary.Rendering.Memory;
using Primary.Rendering.Passes;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.State;
using Primary.RHI;

namespace Primary.Rendering
{
    public sealed class FrameGraph : IDisposable
    {
        private readonly FrameGraphOptions _options;

        private readonly RenderPassManager _renderPassManager;
        private readonly ResourceManager _resourceManager;

        private readonly GraphCompiler _graphCompiler;
        private readonly CompiledRenderPlan[] _renderPlans;

        private readonly RecordingManager _recordingManager;
        private readonly BarrierManager _barrierManager;
        private readonly UploadManager _uploadManager;

        private readonly RasterState _rasterState;
        private readonly ComputeState _computeState;

        private readonly PassDataStorage _passDataStorage;
        private readonly RenderPassBlackboard _renderPassBlackboard;

        private readonly List<PassSubmitInfo> _renderPasses;
        private readonly HashSet<RHISwapChain> _usedSwapChains;

        private bool _framePingPong;

        private bool _disposedValue;

        internal FrameGraph()
        {
            _options = new FrameGraphOptions();

            _renderPassManager = new RenderPassManager(this);
            _resourceManager = new ResourceManager(this);

            _graphCompiler = new GraphCompiler(this);
            _renderPlans = [new CompiledRenderPlan(this), new CompiledRenderPlan(this)];

            _recordingManager = new RecordingManager();
            _barrierManager = new BarrierManager(this);
            _uploadManager = new UploadManager(this);

            _rasterState = new RasterState(this);
            _computeState = new ComputeState(this);

            _passDataStorage = new PassDataStorage();
            _renderPassBlackboard = new RenderPassBlackboard();

            _renderPasses = new List<PassSubmitInfo>();
            _usedSwapChains = new HashSet<RHISwapChain>();

            _framePingPong = false;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _recordingManager.Dispose();
                    _uploadManager.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal CompiledRenderPlan CreateRenderPlan()
        {
            using (new ProfilingScope("CreateRenderPlan"))
            {
                CompiledRenderPlan renderPlan = _renderPlans[FramePingPongValue];
                renderPlan.ClearPlanForPooling();

                SetupPassesForPlan();
                _renderPassManager.AddPassesToPlan(renderPlan);
                _graphCompiler.CompileGraphForExecution(renderPlan);
                _resourceManager.ComputeResourceLifetimes(renderPlan);
                ExecutePassesInPlan(renderPlan);
                _resourceManager.FlushPureResources(renderPlan);
                _barrierManager.BatchAllBarriers(renderPlan);
                _uploadManager.FlushUploads(renderPlan);

                foreach (RHISwapChain swapChain in _usedSwapChains)
                {
                    renderPlan.AddSwapChain(swapChain);
                }

                _framePingPong = !_framePingPong;
                return renderPlan;
            }
        }

        internal void CleanupForEndOfFrame()
        {
            _renderPassManager.CleanupForEndOfFrame();
            _graphCompiler.ClearDataAfterFrame();
            _resourceManager.ClearPreviousFrame();
            _recordingManager.ReclaimRecorders();
            _barrierManager.ClenaupForEndOfFrame();
            _uploadManager.CleanupUploadData();

            _rasterState.ClearState();
            _computeState.ClearState();

            _passDataStorage.ClearEntries(true);
            _renderPassBlackboard.EraseBlackboards();

            _renderPasses.Clear();
            _usedSwapChains.Clear();
        }

        private void SetupPassesForPlan()
        {
            using (new ProfilingScope("SetupPasses"))
            {
                foreach (PassSubmitInfo submitInfo in _renderPasses)
                {
                    submitInfo.Pass.SetupPasses(new FrameGraphBuilder(this, submitInfo.Pass, submitInfo.ContextContainer, _renderPassBlackboard));
                }

                _renderPasses.Clear();
            }
        }

        private void ExecutePassesInPlan(CompiledRenderPlan renderPlan)
        {
            using (new ProfilingScope("ExecutePasses"))
            {
                _rasterState.ClearState();

                ROList<RenderEventInfo> events = renderPlan.Events;

                for (int i = 0; i < events.Count; ++i)
                {
                    RenderEventInfo eventInfo = events[i];
                    if (eventInfo.EventType == RenderEventType.ExecuteDirect || eventInfo.EventType == RenderEventType.ExecuteCompute)
                    {
                        CompiledRenderPass compiledPass = eventInfo.RenderPass1!;
                        RenderPassIndex renderPass = compiledPass.RenderPass!;

                        DescriptorAllocator allocator = eventInfo.EventType == RenderEventType.ExecuteDirect ? renderPlan.DirectDescriptorAllocator : renderPlan.ComputeDescriptorAllocator;

                        using (new ProfilingScope(renderPass.Name))
                        {
                            if (renderPass.PassType == RenderPassType.Raster)
                            {
                                Action<RasterPassContext> rasterExecuteFunc = renderPass.RasterEntrypoint!;

                                RasterCommandBuffer commandBuffer = new RasterCommandBuffer(this, compiledPass, allocator, _rasterState);
                                RasterPassContext context = new RasterPassContext(this, commandBuffer, renderPass.PassData, renderPass.ContextContainer!, _renderPassBlackboard);

                                allocator.RecordDescriptorSets(compiledPass.Recorder);
                                rasterExecuteFunc(context);
                                _rasterState.ClearState();
                            }
                            else if (renderPass.PassType == RenderPassType.Compute)
                            {
                                Action<ComputePassContext> computeExecuteFunc = renderPass.ComputeEntrypoint!;

                                ComputeCommandBuffer commandBuffer = new ComputeCommandBuffer(this, compiledPass, allocator, _computeState);
                                ComputePassContext context = new ComputePassContext(this, commandBuffer, renderPass.PassData, renderPass.ContextContainer!, _renderPassBlackboard);

                                allocator.RecordDescriptorSets(compiledPass.Recorder);
                                computeExecuteFunc(context);
                                _computeState.ClearState();
                            }
                        }
                    }
                }

                for (int i = events.Count - 1; i >= 0; --i)
                {
                    RenderEventInfo eventInfo = events[i];
                    if (eventInfo.EventType == RenderEventType.ExecuteDirect)
                    {
                        CompiledRenderPass renderPass = eventInfo.RenderPass1!;
                        foreach (RHISwapChain swapChain in _usedSwapChains)
                        {
                            TextureHandle handle = _resourceManager.GetResourceHandle(swapChain.Backbuffers[swapChain.CurrentBackbufferIndex]).AsTexture();
                            renderPass.BarrierGroup.RequestBarrier(handle, renderPass.Recorder.CommandOffset, ResourceBarrierSync.Draw, ResourceBarrierAccess.Common, true);
                        }

                        break;
                    }
                }
            }
        }

        #region Pass interface
        internal void PushRenderPass(IRenderPass renderPass, RenderContextContainer contextContainer) => _renderPasses.Add(new PassSubmitInfo(renderPass, contextContainer));
        internal RenderPassIndex RegisterNewPass(IRenderPass renderPass, RenderPassType passType, string name, RenderContextContainer contextContainer) => _renderPassManager.EnqueuePass(renderPass, passType, name, contextContainer);

        internal T GetBlankPassData<T>() where T : class, IPassData, new() => _passDataStorage.GetPassData<T>();

        internal BufferHandle ImportBufferAsPure(RHIBuffer buffer) => _resourceManager.ImportBufferAsPure(buffer);
        internal TextureHandle ImportTextureAsPure(RHITexture texture) => _resourceManager.ImportTextureAsPure(texture);

        internal void UseSwapChain(RHISwapChain swapChain) => _usedSwapChains.Add(swapChain);
        #endregion

        public FrameGraphOptions Options => _options;

        public Passes.RenderPassManager RenderPassManager => _renderPassManager;
        public ResourceManager ResourceManager => _resourceManager;

        public GraphCompiler GraphCompiler => _graphCompiler;

        public RecordingManager RecordingManager => _recordingManager;
        public UploadManager UploadManager => _uploadManager;

        public RenderPassBlackboard Blackboard => _renderPassBlackboard;

        public bool FramePingPong => _framePingPong;
        public int FramePingPongValue => Unsafe.BitCast<bool, byte>(_framePingPong);

        private readonly record struct PassSubmitInfo(IRenderPass Pass, RenderContextContainer ContextContainer);
    }
}
