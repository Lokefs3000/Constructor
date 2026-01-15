using CommunityToolkit.HighPerformance;
using Primary.Profiling;
using Primary.Rendering.Data;
using Primary.Rendering.Memory;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI2;
using Primary.Utility;

namespace Primary.Rendering
{
    public sealed class RenderPassManager : IDisposable
    {
        private RenderPass _renderPass;
        private RenderPassCompiler _renderPassCompiler;
        private FrameGraphTimeline _timeline;
        private FrameGraphResources _resources;
        private FrameGraphRecorder _recorder;
        private FrameGraphState _state;
        private FrameGraphSetup _setup;

        private SequentialLinearAllocator _intermediateAllocator;
        private RenderPassErrorReporter _errorReporter;
        private RasterPassContext _rasterContext;
        private ComputePassContext _computeContext;

        private List<IRenderPass> _activePasses;
        private List<FrameGraphCommands> _commands;

        private bool _disposedValue;

        internal RenderPassManager(RenderingManager manager)
        {
            _renderPass = new RenderPass(this);
            _renderPassCompiler = new RenderPassCompiler();
            _timeline = new FrameGraphTimeline();
            _resources = new FrameGraphResources(manager);
            _recorder = new FrameGraphRecorder(this);
            _state = new FrameGraphState();
            _setup = new FrameGraphSetup();

            _intermediateAllocator = new SequentialLinearAllocator(ushort.MaxValue /*65kb*/);
            _errorReporter = new RenderPassErrorReporter();
            _rasterContext = new RasterPassContext(_errorReporter, _intermediateAllocator, _resources, manager.ContextContainer);
            _computeContext = new ComputePassContext(_errorReporter, _intermediateAllocator, _resources, manager.ContextContainer);

            _activePasses = new List<IRenderPass>();
            _commands = new List<FrameGraphCommands>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _resources.Dispose();
                    _timeline.Dispose();
                    _recorder.Dispose();

                    _intermediateAllocator.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearInternals()
        {
            _renderPass.ClearInternals();
            _timeline.ClearTimeline();
            _resources.ClearNewFrame();
            _recorder.ClearForFrame();
            _state.ClearForFrame();
            _setup.ClearForFrame();
            _intermediateAllocator.Reset();
            _commands.Clear();
        }

        internal void SetupPasses(RenderContextContainer contextContainer)
        {
            using (new ProfilingScope("Setup"))
            {
                foreach (IRenderPass renderPass in _activePasses)
                {
                    renderPass.SetupRenderPasses(_renderPass, contextContainer);
                }
            }
        }

        internal void CompilePasses(RenderContextContainer contextContainer)
        {
            using (new ProfilingScope("Compile"))
            {
                FrameGraphTexture texture = contextContainer.Get<RenderCameraData>()!.ColorTexture;
                _renderPassCompiler.Compile(texture, _renderPass.Passes, _timeline, _resources, _state);
            }
        }

        internal void ExecutePasses(RenderContextContainer contextContainer)
        {
            using (new ProfilingScope("Execute"))
            {
                ReadOnlySpan<RenderPassDescription> submittedPasses = _renderPass.Passes;
                foreach (int passIndex in _timeline.Passes)
                {
                    ref readonly RenderPassDescription desc = ref submittedPasses[passIndex];
                    if (desc.Type == RenderPassType.Graphics)
                    {
                        CommandRecorder recorder = _recorder.GetNewRecorder(passIndex);
                        RenderPassStateData stateData = _state.GetStateData(passIndex);

                        stateData.SetupState(in desc);
                        _rasterContext.SetupContext(stateData, recorder);

                        submittedPasses[passIndex].Function?.Invoke(_rasterContext, _renderPass.GetPassData(desc.PassDataType!) ?? throw new NullReferenceException());
                        recorder.FinishRecording();

                        _commands.Add(new FrameGraphCommands(recorder));
                    }
                    else if (desc.Type == RenderPassType.Compute)
                    {
                        CommandRecorder recorder = _recorder.GetNewRecorder(passIndex);
                        RenderPassStateData stateData = _state.GetStateData(passIndex);

                        stateData.SetupState(in desc);
                        _computeContext.SetupContext(stateData, recorder);

                        submittedPasses[passIndex].Function?.Invoke(_computeContext, _renderPass.GetPassData(desc.PassDataType!) ?? throw new NullReferenceException());
                        recorder.FinishRecording();

                        _commands.Add(new FrameGraphCommands(recorder));
                    }
                }
            }
        }

        internal void SetWindowOutput(RHISwapChain swapChain, FrameGraphTexture texture)
        {
            _setup.OutputSwapChain = swapChain;
            _setup.DestinationTexture = texture;
        }

        /// <summary>Not thread-safe</summary>
        public void AddRenderPass<T>() where T : class, IRenderPass, new()
        {
            if (!_activePasses.Exists((x) => x is T))
            {
                _activePasses.Add(new T());
            }
        }

        /// <summary>Not thread-safe</summary>
        public void RemoveRenderPass<T>() where T : class, IRenderPass, new()
        {
            _activePasses.RemoveWhere((x) => x is T);
        }

        internal RenderPass RenderPass => _renderPass;
        internal RenderPassCompiler Compiler => _renderPassCompiler;

        internal FrameGraphTimeline Timeline => _timeline;
        internal FrameGraphResources Resources => _resources;
        internal FrameGraphRecorder Recorder => _recorder;
        internal FrameGraphSetup Setup => _setup;

        public ReadOnlySpan<RenderPassDescription> CurrentPasses => _renderPass.Passes;
        public ReadOnlySpan<FrameGraphCommands> Commands => _commands.AsSpan();
    }
}
