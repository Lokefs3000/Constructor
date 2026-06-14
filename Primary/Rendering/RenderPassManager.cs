using CommunityToolkit.HighPerformance;
using Primary.Common.Memory;
using Primary.Profiling;
using Primary.Rendering.Data;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Reflection;
using Primary.Rendering.Resources;
using Primary.Rendering.State;
using Primary.RHI;
using Primary.Utility;

namespace Primary.Rendering
{
    public sealed class RenderPassManager : IDisposable
    {
        private RenderPass _renderPass;
        private RenderPassCompiler _renderPassCompiler;
        private RenderPassSetupCache _renderPassSetupCache;
        private FrameGraphTimeline _timeline;
        private FrameGraphResources _resources;
        private FrameGraphRecorder _recorder;
        private FrameGraphState _state;
        private FrameGraphSetup _setup;

        private RasterState _rasterState;
        private ComputeState _computeState;

        private LinearBlockAllocator _intermediateAllocator;
        private RenderPassErrorReporter _errorReporter;
        private RasterPassContext _rasterContext;
        private ComputePassContext _computeContext;

        private List<RenderPassData> _activePasses;
        private List<FrameGraphCommands> _commands;

        private bool _disposedValue;

        internal RenderPassManager(RenderingManager manager)
        {
            _renderPass = new RenderPass(this);
            _renderPassCompiler = new RenderPassCompiler();
            _renderPassSetupCache = new RenderPassSetupCache();
            _timeline = new FrameGraphTimeline();
            _resources = new FrameGraphResources(manager);
            _recorder = new FrameGraphRecorder(this);
            _state = new FrameGraphState();
            _setup = new FrameGraphSetup();

            _rasterState = new RasterState();
            _computeState = new ComputeState();

            _intermediateAllocator = new LinearBlockAllocator(ushort.MaxValue /*65kb*/);
            _errorReporter = new RenderPassErrorReporter();
            _rasterContext = new RasterPassContext(_errorReporter, _intermediateAllocator, _resources, _rasterState);
            _computeContext = new ComputePassContext(_errorReporter, _intermediateAllocator, _resources, _computeState);

            _activePasses = new List<RenderPassData>();
            _commands = new List<FrameGraphCommands>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (RenderPassData passData in _activePasses)
                    {
                        if (passData.Pass is IDisposable disposable)
                            disposable.Dispose();
                    }

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

            _rasterState.ClearState();
            _computeState.ClearState();
        }

        internal void ClearLocalData()
        {
            _renderPass.ClearLocalData();
            _timeline.ClearLocalData();
            _resources.ClearLocalData();

            _rasterState.ClearState();
            _computeState.ClearState();
        }

        internal void SetupPasses(RenderPassRunContext runContext, RenderContextContainer contextContainer)
        {
            using (new ProfilingScope("Setup"))
            {
                foreach (RenderPassData renderPass in _activePasses)
                {
                    RenderPassSetupAttribute? setup = renderPass.SetupData;
                    if ((setup?.RunContext ?? RenderPassRunContext.PerCamera) != runContext)
                    {
                        continue;
                    }

                    renderPass.Pass.SetupRenderPasses(_renderPass, contextContainer);
                }
            }
        }

        internal void CompilePasses()
        {
            using (new ProfilingScope("Compile"))
            {
                _renderPassCompiler.Compile(_renderPass.Passes, _renderPass.Groups, _timeline, _resources, _state);
            }
        }

        internal void ExecutePasses()
        {
            using (new ProfilingScope("Execute"))
            {
                ReadOnlySpan<RenderPassDescription> submittedPasses = _renderPass.Passes;
                ReadOnlySpan<RenderPassGroup> passGroups = _renderPass.Groups;

                foreach (int passIndex in _timeline.Passes)
                {
                    int actualIndex = passIndex - _timeline.PassIndexOffset;

                    ref readonly RenderPassDescription desc = ref submittedPasses[actualIndex];
                    ref readonly RenderPassGroup group = ref passGroups[desc.GroupIndex];

                    if (desc.Type == RenderPassType.Graphics)
                    {
                        CommandRecorder recorder = _recorder.GetNewRecorder(passIndex);
                        RenderPassStateData stateData = _state.GetStateData(passIndex);

                        stateData.SetupState(in desc);
                        _rasterContext.SetupContext(stateData, recorder, group.Context);
                        _rasterState.SoftResetForNextPass();

                        submittedPasses[actualIndex].Function?.Invoke(desc.RealFunction!, _rasterContext, desc.PassData);

                        _commands.Add(new FrameGraphCommands(recorder));
                    }
                    else if (desc.Type == RenderPassType.Compute)
                    {
                        CommandRecorder recorder = _recorder.GetNewRecorder(passIndex);
                        RenderPassStateData stateData = _state.GetStateData(passIndex);

                        stateData.SetupState(in desc);
                        _computeContext.SetupContext(stateData, recorder, group.Context);
                        _computeState.SoftResetForNextPass();

                        submittedPasses[actualIndex].Function?.Invoke(desc.RealFunction!, _computeContext, desc.PassData);

                        _commands.Add(new FrameGraphCommands(recorder));
                    }
                }
            }
        }

        /// <summary>Not thread-safe</summary>
        internal void PushCurrentPassGroup(FrameGraphTexture finalTexture, RenderPassRunContext runContext, RenderContextContainer context) => _renderPass.PushCurrentPassGroup(finalTexture, runContext, context);
        /// <summary>Not thread-safe</summary>
        internal void ClearCurrentPassGroup() => _renderPass.ClearCurrentPassGroup();

        /// <summary>Not thread-safe</summary>
        public void AddRenderPass<T>() where T : class, IRenderPass, new()
        {
            if (!_activePasses.Exists((x) => x.Pass is T))
            {
                _activePasses.Add(new RenderPassData(new T(), _renderPassSetupCache.TryGetSetupData<T>()));
            }
        }

        /// <summary>Not thread-safe</summary>
        public void RemoveRenderPass<T>() where T : class, IRenderPass, new()
        {
            if (_activePasses.RemoveWhere((x) => x.Pass is T, out RenderPassData data) && data.Pass is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        internal RenderPass RenderPass => _renderPass;
        internal RenderPassCompiler Compiler => _renderPassCompiler;

        internal FrameGraphTimeline Timeline => _timeline;
        internal FrameGraphResources Resources => _resources;
        internal FrameGraphRecorder Recorder => _recorder;
        internal FrameGraphSetup Setup => _setup;

        public ReadOnlySpan<RenderPassDescription> CurrentPasses => _renderPass.Passes;
        public ReadOnlySpan<FrameGraphCommands> Commands => _commands.AsSpan();

        private readonly record struct RenderPassData(IRenderPass Pass, RenderPassSetupAttribute? SetupData);
    }
}
