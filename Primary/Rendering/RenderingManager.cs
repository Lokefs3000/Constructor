using Arch.Core.Extensions;
using Primary.Common;
using Primary.Components;
using Primary.Editor;
using Primary.Profiling;
using Primary.Rendering.Batching;
using Primary.Rendering.Data;
using Primary.Rendering.Forward;
using Primary.Rendering.Pooling;
using Primary.Rendering.Raw;
using Primary.RHI;
using Serilog;
using System.Runtime.CompilerServices;

namespace Primary.Rendering
{
    public class RenderingManager : IDisposable
    {
        private GraphicsDevice _graphicsDevice;
        private static WeakReference s_gd = new WeakReference(null);

        private SwapChainCache _swapChainCache;
        private RenderBatcher _renderBatcher;
        private RenderPassManager _renderPassManager;
        private RenderScene _renderScene;
        private FrameCollector _frameCollector;
        private FrameUploadManager _frameUploadManager;
        private CommandBufferPool _commandBufferPool;
        private RenderPassData _renderPassData;
        private RenderTargetPool _renderTargetPool;
        private Blitter _blitter;

        private IRenderPath _path;

        private Window? _defaultWindow;

        private bool _disposedValue;

        public RenderingManager()
        {
            _graphicsDevice = GraphicsDeviceFactory.Create(GraphicsAPI.Direct3D12, new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [RHI] {Message:lj}{NewLine}{Exception}").CreateLogger());
            s_gd.Target = _graphicsDevice;

            _swapChainCache = new SwapChainCache(_graphicsDevice);
            _renderBatcher = new RenderBatcher();
            _renderPassManager = new RenderPassManager();
            _renderScene = new RenderScene();
            _frameCollector = new FrameCollector();
            _frameUploadManager = new FrameUploadManager(_graphicsDevice);
            _commandBufferPool = new CommandBufferPool(_graphicsDevice);
            _renderPassData = new RenderPassData(new RenderPassViewportData(), new RenderPassLightingData());
            _renderTargetPool = new RenderTargetPool(_graphicsDevice);
            _blitter = new Blitter();

            _path = new ForwardRenderPath(this);

            _defaultWindow = null;

            _renderPassManager.AddRenderPass<ShadowPass>();
            _renderPassManager.AddRenderPass<ForwardOpaquePass>();
            _renderPassManager.AddRenderPass<FinalBlitPass>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _graphicsDevice.SynchronizeDevice(SynchronizeDeviceTargets.All);

                    _path.Dispose();

                    s_gd.Target = null;

                    _renderTargetPool.Dispose();
                    _commandBufferPool.Dispose();
                    _frameUploadManager.Dispose();
                    _renderPassManager.Dispose();
                    _renderBatcher.Dispose();
                    _swapChainCache.Dispose();
                    _graphicsDevice.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void ExecuteRender()
        {
            if (_defaultWindow == null)
                return;

            using (new ProfilingScope("ExecuteRender"))
            {
                SwapChain swapChain = _swapChainCache.GetOrAddDefault(_defaultWindow);

                //_graphicsDevice.SynchronizeDevice(SynchronizeDeviceTargets.All);

                _graphicsDevice.BeginFrame();

                _commandBufferPool.PrepareNewFrame();
                _renderPassManager.ReorganizePassesIfRequired();
                _frameCollector.SetupScene(_renderScene);
                _frameCollector.CollectWorld(_renderBatcher);

                //_frameUploadManager.UploadPending();
                //_frameUploadManager.OpenBuffersForFrame();

                Span<RSOutputViewport> viewports = _renderScene.Viewports;
                for (int i = 0; i < viewports.Length; i++)
                {
                    using (new ProfilingScope("RTViewport"))
                    {
                        SetupRenderState(ref viewports[i]);

                        _path.PreparePasses(_renderPassData);
                        _renderPassManager.ExecuteAllPasses(_path, _renderPassData);
                        _path.CleanupPasses(_renderPassData);
                    }
                }

                //_frameUploadManager.SubmitBuffersForEndOfFrame();

                _renderBatcher.ClearBatchData();
                _renderScene.ClearInternalData();

                swapChain.Present(PresentParameters.None);
            }

            using (new ProfilingScope("FinishFrame"))
            {
                _graphicsDevice.FinishFrame();
            }
        }

        public void EmitDebugStatistics(DebugDataContainer container)
        {
            _path?.EmitDebugStatistics(container);
        }

        private void SetupRenderState(ref RSOutputViewport outputViewport)
        {
            {
                RenderPassViewportData rpViewportData = _renderPassData.Get<RenderPassViewportData>()!;
                rpViewportData.CameraRenderTarget = _renderTargetPool.GetOrCreate(outputViewport.Id, outputViewport.ClientSize);
                rpViewportData.BackBufferRenderTarget = _swapChainCache.GetOrAddDefault(_defaultWindow!).BackBuffer;
                rpViewportData.RefCameraSetter = outputViewport.RootEntity.Get<Camera>();
                rpViewportData.View = outputViewport.ViewMatrix;
                rpViewportData.Projection = outputViewport.ProjectionMatrix;
                rpViewportData.VP = outputViewport.ViewMatrix * outputViewport.ProjectionMatrix;
                rpViewportData.ViewPosition = outputViewport.ViewPosition;
                rpViewportData.ViewDirection = outputViewport.ViewDirection;
            }

            {
                RenderPassLightingData rpLightingData = _renderPassData.Get<RenderPassLightingData>()!;
            }
        }

        public Window? DefaultWindow { get => _defaultWindow; set => _defaultWindow = value; }

        public SwapChainCache SwapChainCache => _swapChainCache;
        public GraphicsDevice GraphicsDevice => _graphicsDevice;
        public RenderBatcher RenderBatcher => _renderBatcher;
        public RenderPassManager RenderPassManager => _renderPassManager;
        public FrameCollector FrameCollector => _frameCollector;

        internal RenderScene RenderScene => _renderScene;

        public IRenderPath RenderPath => _path;

        internal event Action<IDebugCallbacks>? EmitDebugData;

        public static GraphicsDevice Device => NullableUtility.ThrowIfNull((GraphicsDevice?)s_gd.Target);
    }
}
