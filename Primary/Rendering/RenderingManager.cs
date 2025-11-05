using Arch.Core.Extensions;
using Primary.Assets;
using Primary.Common;
using Primary.Components;
using Primary.Editor;
using Primary.Profiling;
using Primary.Rendering.Batching;
using Primary.Rendering.Pass;
using Primary.Rendering.Pooling;
using Primary.Rendering.PostProcessing;
using Primary.Rendering.Raw;
using Primary.Rendering.Tree;
using Primary.RenderLayer;
using Primary.RHI;
using Serilog;
using System.Numerics;

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
        private RenderTreeManager _renderTreeManager;
        private RenderTreeCollector _renderTreeCollector;
        private EffectManager _effectManager;

        private RenderPass _renderPass;

        private MaterialAsset _missingMaterial;

        private IRenderPath _path;

        private Window? _defaultWindow;

        private RenderingConfig _config;

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
            _renderTreeManager = new RenderTreeManager();
            _renderTreeCollector = new RenderTreeCollector();
            _effectManager = new EffectManager();

            _renderPass = new RenderPass();

            _missingMaterial = AssetManager.LoadAsset<MaterialAsset>("Engine/Materials/Missing.mat", true)!;

            _path = new ForwardRenderPath(this);

            _defaultWindow = null;

            _config = new RenderingConfig();
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

                    _effectManager.Dispose();
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

            SwapChain swapChain = _swapChainCache.GetOrAddDefault(_defaultWindow);

            using (new ProfilingScope("ExecuteRender"))
            {
                //_graphicsDevice.SynchronizeDevice(SynchronizeDeviceTargets.All);

                _graphicsDevice.BeginFrame();

                _renderTreeManager.UpdateForFrame();
                _commandBufferPool.PrepareNewFrame();
                _renderPassManager.ReorganizePassesIfRequired();
                _frameCollector.SetupScene(_renderScene);
                //_frameCollector.CollectWorld(_renderBatcher, _missingMaterial);
                _renderTreeCollector.CollectTrees(_renderBatcher, _renderTreeManager);
                _renderBatcher.BatchCollected(_renderTreeCollector);

                //_frameUploadManager.UploadPending();
                //_frameUploadManager.OpenBuffersForFrame();

                PreRender?.Invoke();

                Span<RSOutputViewport> viewports = _renderScene.Viewports;
                if (viewports.Length > 0)
                {
                    for (int i = 0; i < viewports.Length; i++)
                    {
                        using (new ProfilingScope("RTViewport"))
                        {
                            using (new ProfilingScope("Prepare"))
                            {
                                SetupRenderState(ref viewports[i]);
                                _effectManager.UpdateActiveVolumes();

                                _path.PreparePasses(_renderPassData);
                                _path.ExecutePasses(_renderPass);
                            }

                            using (new ProfilingScope("Dispatch"))
                            {
                                DispatchRenderPasses();
                            }
                        }
                    }
                }

                using (new ProfilingScope("Post"))
                {
                    PostRender?.Invoke(_renderPass);
                    if (!_renderPass.IsEmpty)
                    {
                        using (new ProfilingScope("Dispatch"))
                        {
                            DispatchRenderPasses();
                        }
                    }
                }

                //_frameUploadManager.SubmitBuffersForEndOfFrame();

                _renderBatcher.CleanupPostFrame();
                _renderScene.ClearInternalData();
            }

            swapChain.Present(PresentParameters.None);

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
                Vector2 outputSize = outputViewport.ClientSize;
                if (outputSize.X >= 1.0f && outputSize.Y >= 1.0f)
                    outputSize = _config.OutputViewport;

                RenderPassViewportData rpViewportData = _renderPassData.Get<RenderPassViewportData>()!;
                rpViewportData.CameraRenderTarget = _renderTargetPool.GetOrCreate(outputViewport.Id, outputSize);
                rpViewportData.BackBufferRenderTarget = _swapChainCache.GetOrAddDefault(_defaultWindow!).BackBuffer;
                rpViewportData.RefCameraSetter = outputViewport.RootEntity.Get<Camera>();
                rpViewportData.View = outputViewport.ViewMatrix;
                rpViewportData.Projection = outputViewport.ProjectionMatrix;
                rpViewportData.VP = outputViewport.ViewMatrix * outputViewport.ProjectionMatrix;
                rpViewportData.ViewPosition = outputViewport.ViewPosition;
                rpViewportData.ViewDirection = outputViewport.ViewDirection;

                //cfg
                if (!_config.OutputRenderTarget.IsNull)
                    rpViewportData.BackBufferRenderTarget = _config.OutputRenderTarget.RHIRenderTarget!;
            }

            {
                RenderPassLightingData rpLightingData = _renderPassData.Get<RenderPassLightingData>()!;
            }
        }

        private void DispatchRenderPasses()
        {
            IReadOnlyList<IPassDescription> descriptions = _renderPass.Descriptions;

            //TODO: implement auto threading of passes once a task scheduler is actually implemented

            foreach (IPassDescription desc in descriptions)
            {
                desc.ExecuteInternal(_renderPassData);
            }

            _renderPass.ClearForNextFrame();
        }

        public Window? DefaultWindow { get => _defaultWindow; set => _defaultWindow = value; }

        public SwapChainCache SwapChainCache => _swapChainCache;
        public GraphicsDevice GraphicsDevice => _graphicsDevice;
        public RenderBatcher RenderBatcher => _renderBatcher;
        public RenderPassManager RenderPassManager => _renderPassManager;
        public FrameCollector FrameCollector => _frameCollector;
        public RenderTreeManager RenderTreeManager => _renderTreeManager;
        public EffectManager EffectManager => _effectManager;

        public RenderScene RenderScene => _renderScene;

        public IRenderPath RenderPath => _path;

        public ref RenderingConfig Configuration => ref _config;

        public event Action? PreRender;
        public event Action<RenderPass>? PostRender;

        internal event Action<IDebugCallbacks>? EmitDebugData;

        public static GraphicsDevice Device => NullableUtility.ThrowIfNull((GraphicsDevice?)s_gd.Target);

        /// <summary>Not thread-safe</summary>
        //TODO: better implementation
        public static IDebugRenderer? DebugRenderer { get; set; }
    }

    public struct RenderingConfig
    {
        public GfxRenderTarget OutputRenderTarget;
        public Vector2 OutputViewport;

        public RenderingMode RenderMode;
    }

    public enum RenderingMode : byte
    {
        Lit = 0,
        Unlit,
        Wireframe,
        Normals,
        Lighting,
        DetailLighting,
        Reflections,
        ShaderComplexity,
        Overdraw
    }
}
