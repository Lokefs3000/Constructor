using Primary.Profiling;
using Primary.Rendering2.Data;
using Primary.Rendering2.Memory;
using Primary.Rendering2.Pass;
using Primary.Rendering2.Recording;
using Primary.Rendering2.Resources;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2
{
    public sealed class RenderPassManager : IDisposable
    {
        private RenderPass _renderPass;
        private RenderPassCompiler _renderPassCompiler;
        private FrameGraphTimeline _timeline;
        private FrameGraphRecorder _recorder;
        private FrameGraphState _state;

        private SequentialLinearAllocator _intermediateAllocator;
        private RenderPassErrorReporter _errorReporter;
        private RasterPassContext _rasterContext;

        private List<IRenderPass> _activePasses;

        private bool _disposedValue;

        internal RenderPassManager()
        {
            _renderPass = new RenderPass();
            _renderPassCompiler = new RenderPassCompiler();
            _timeline = new FrameGraphTimeline();
            _recorder = new FrameGraphRecorder();
            _state = new FrameGraphState();

            _intermediateAllocator = new SequentialLinearAllocator(ushort.MaxValue /*65kb*/);
            _errorReporter = new RenderPassErrorReporter();
            _rasterContext = new RasterPassContext(_errorReporter, _intermediateAllocator);

            _activePasses = new List<IRenderPass>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
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
            _recorder.ClearForFrame();
            _state.ClearForFrame();
            _intermediateAllocator.Reset();
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
                _renderPassCompiler.Compile(texture, _renderPass.Passes, _timeline, _state);
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
                        CommandRecorder recorder = _recorder.GetNewRecorder();
                        _rasterContext.SetupContext(_state.GetStateData(passIndex), recorder);

                        submittedPasses[passIndex].Function?.Invoke(_rasterContext, _renderPass.GetPassData(desc.PassDataType!) ?? throw new NullReferenceException());
                        recorder.FinishRecording();
                    }
                }
            }
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
    }
}
