using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Rendering.Passes;
using Primary.Rendering.Recording;
using Primary.Rendering.Statistics;

namespace Primary.Rendering
{
    public sealed class CompiledRenderPass : IDisposable
    {
        private readonly FrameGraph _frameGraph;

        private readonly CommandRecorder _recorder;
        private readonly BarrierGroup _barrierGroup;
        private readonly ObjectsCache _objectsCache;

        private readonly List<RenderBarrier> _barriers;

        private readonly HashSet<int> _uninitializedTextures;

        private RenderPassType _passType;
        private RenderPassIndex? _renderPass;

        private RenderPassStats _stats;

        private bool _disposedValue;

        internal CompiledRenderPass(FrameGraph frameGraph)
        {
            _frameGraph = frameGraph;

            _recorder = new CommandRecorder();
            _barrierGroup = new BarrierGroup();
            _objectsCache = new ObjectsCache();

            _barriers = new List<RenderBarrier>();

            _uninitializedTextures = new HashSet<int>();

            _passType = RenderPassType.Raster;
            _renderPass = null;

            _stats = default;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _recorder.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void SetupPassForUse(RenderPassIndex index)
        {
            _passType = index.PassType;
            _renderPass = index;

            _stats = default;
        }

        internal void ClearPassForPooling()
        {
            _recorder.ClearRecording();
            _barrierGroup.ClearBarrierData();
            _objectsCache.ClearStoredObjects();

            _barriers.Clear();

            _uninitializedTextures.Clear();

            _renderPass = null;
        }

        internal void AddBarrier(RenderBarrier barrier) => _barriers.Add(barrier);
        internal void AddBarriers(ReadOnlySpan<RenderBarrier> barriers) => _barriers.AddRange(barriers);

        internal void AddUninitializedRenderTarget(TextureHandle handle)
        {
            // _barrierGroup.RequestBarrier(handle, 0, ResourceBarrierSync.RenderTarget, ResourceBarrierAccess.RenderTarget);
            _uninitializedTextures.Add(handle.Index);
        }

        internal void AddUninitializedDepthStencil(TextureHandle handle)
        {
            // _barrierGroup.RequestBarrier(handle, 0, ResourceBarrierSync.DepthStencil, ResourceBarrierAccess.DepthStencilWrite);
            _uninitializedTextures.Add(handle.Index);
        }

        internal void InitializeRenderTarget(TextureHandle handle) => _uninitializedTextures.Remove(handle.Index);
        internal void InitializeDepthStencil(TextureHandle handle) => _uninitializedTextures.Remove(handle.Index);

        public override string ToString() => _renderPass?.Name ?? GetType().FullName!;

        public FrameGraph FrameGraph => _frameGraph;

        internal CommandRecorder Recorder => _recorder;
        internal BarrierGroup BarrierGroup => _barrierGroup;
        internal ObjectsCache ObjectsCache => _objectsCache;

        internal ROList<RenderBarrier> Barriers => _barriers;

        internal ROHashSet<int> UninitializedTextures => _uninitializedTextures;

        public RenderPassType PassType { get => _passType; internal set => _passType = value; }
        public RenderPassIndex? RenderPass => _renderPass;

        public ref RenderPassStats Stats => ref _stats;
    }
}
