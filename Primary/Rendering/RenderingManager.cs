using System.Diagnostics;
using CommunityToolkit.Diagnostics;
using Primary.Assets;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Profiling;
using Primary.Rendering.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Diagnostics;
using Primary.Rendering.Passes;
using Primary.Rendering.Playback;
using Primary.Rendering.Tree;
using Primary.Rendering.Utility;
using Primary.RHI;
using Primary.Scenes;
using Primary.Threading;
using Primary.Utility;
using Primary.Windowing;

namespace Primary.Rendering
{
    public class RenderingManager : IDisposable
    {
        private readonly RHIDevice _graphicsDevice;
        private readonly RHISampler _defaultSampler;
        private readonly IPlaybackDevice _playbackDevice;

        private readonly FrameGraph _frameGraph;
        private readonly ShaderGlobalsManager _globalsManager;
        private readonly SwapChainCache _swapChainCache;
        private readonly RenderContextPool _renderContextPool;

        private readonly RenderWorld _renderWorld;
        private readonly OctreeManager _octreeManager;
        private readonly BatchingManager _batchingManager;

        private readonly DebugManager _debugManager;

        private readonly RenderPrimitives _renderPrimitives;

        private readonly List<IRenderStage> _renderStages;

        private readonly SetupCameraPass _setupCameraPass;
        private readonly SetupWindowPass _setupWindowPass;
        private readonly PresentOutputPass _presentPass;

        private readonly TransientEventList<CompiledRenderPlan> _inspectRenderPlan;

        private RenderContextContainer? _currentContextContainer;

        private readonly HashSet<Window> _renderedWindows;

        private JobHandle _currentPlaybackJob;

        private bool _disposedValue;

        public RenderingManager()
        {
            _graphicsDevice = RHIDeviceFactory.CreateDefaultApi(new RHIDeviceDescription { EnableValidation = Engine.IsDebugBuild && !AppArguments.HasArgument("rhi-nodebug") }, EngLog.RHI);
            _defaultSampler = _graphicsDevice.CreateSampler(new RHISamplerDescription(), "Default")!;
            _playbackDevice = PlaybackDeviceFactory.CreateDevice(_graphicsDevice);

            _frameGraph = new FrameGraph();
            _globalsManager = new ShaderGlobalsManager(_frameGraph);
            _swapChainCache = new SwapChainCache(this);
            _renderContextPool = new RenderContextPool();

            _renderWorld = new RenderWorld();
            _octreeManager = new OctreeManager();
            _batchingManager = new BatchingManager(this);

            _debugManager = new DebugManager();

            _renderPrimitives = new RenderPrimitives();

            _renderStages = new List<IRenderStage>();

            _setupCameraPass = new SetupCameraPass();
            _setupWindowPass = new SetupWindowPass();
            _presentPass = new PresentOutputPass();

            _inspectRenderPlan = new TransientEventList<CompiledRenderPlan>();

            _currentContextContainer = null;

            _renderedWindows = new HashSet<Window>();

            _currentPlaybackJob = JobHandle.Completed;

            _debugManager.TrySetupAfterRenderInit(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (!_currentPlaybackJob.HasCompleted)
                        _currentPlaybackJob.WaitForCompletion();

                    foreach (IRenderStage stage in _renderStages)
                    {
                        stage.Uninstall(this);
                    }

                    _renderStages.Clear();

                    _presentPass.Dispose();

                    _renderPrimitives.Dispose();
                    _swapChainCache.Dispose();
                    _frameGraph.Dispose();

                    _playbackDevice.Dispose();
                    _defaultSampler.Dispose();
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

        /// <summary>Not thread-safe</summary>
        public void InstallRenderStage(IRenderStage stage)
        {
            if (_renderStages.AddUnique(stage))
            {
                stage.Install(this);
            }
        }

        /// <summary>Not thread-safe</summary>
        public void UninstallRenderStage(IRenderStage stage)
        {
            if (_renderStages.Remove(stage))
            {
                stage.Uninstall(this);
            }
        }

        public void Render()
        {
            using (new ProfilingScope("Render"))
            {
                using (new ProfilingScope("Prepare"))
                {
                    _renderedWindows.Clear();

                    _graphicsDevice.IncrementFrame();

                    _renderContextPool.ResetPoolForNewRender();
                    _octreeManager.QueryPending();
                    _renderWorld.SetupWorld();
                }

                using (new ProfilingScope("Setup"))
                {
                    if (_renderStages.Count > 0)
                    {
                        for (int i = 0; i < _renderWorld.Outputs.Length; i++)
                        {
                            RenderOutputData outputData = _renderWorld.Outputs[i];

                            RenderFromPerspective(outputData, out TextureHandle currentTexture);
                            if (i + 1 == _renderWorld.Outputs.Length || _renderWorld.Outputs[i + 1].Window != outputData.Window)
                            {
                                if (currentTexture.IsImported)
                                    currentTexture = TextureHandle.Null;

                                RenderFromWindow(outputData.Window, currentTexture);

                                _renderedWindows.Add(outputData.Window);
                            }
                        }

                        foreach (var kvp in WindowManager.Instance.Windows)
                        {
                            Window window = kvp.Value;
                            if (window.IsShown && _renderedWindows.Add(window))
                            {
                                RenderFromWindow(window, TextureHandle.Null);
                            }
                        }
                    }
                }

                CompiledRenderPlan renderPlan = _frameGraph.CreateRenderPlan();
                _inspectRenderPlan.InvokeAndClear(renderPlan);

                if (renderPlan.RenderPasses.Count > 0)
                {
                    if (!_currentPlaybackJob.HasCompleted)
                        _currentPlaybackJob.WaitForCompletion();
                    _currentPlaybackJob = _playbackDevice.ExecuteRenderPlan(renderPlan);

                    if (!_frameGraph.Options.AllowMultithreadedPlayback)
                        _currentPlaybackJob.WaitForCompletion();
                }

                _globalsManager.CleanupTransitional();
                _frameGraph.CleanupForEndOfFrame();

                _debugManager.FinishCurrentRender();

                _renderedWindows.Clear();
            }
        }

        private void RenderFromPerspective(RenderOutputData outputData, out TextureHandle usedTexture)
        {
            using (new ProfilingScope(Engine.IsDebugBuild ? $"Cam-{outputData.Entity.Name}" : "Cam-Default"))
            {
                usedTexture = TextureHandle.Null;

                SetupContextForOutput(outputData, out RenderContextContainer context);

                foreach (IRenderStage stage in _renderStages)
                {
                    stage.FlushPassesForCamera(this, _frameGraph.Blackboard, context);
                }

                _debugManager.UpdatePerCamera(this);

                TextureHandle currentTexture = context.Get<RenderCameraData>()!.ColorTexture;
                if (!currentTexture.IsImported)
                {
                    PushRenderPass(_presentPass);
                }

                _currentContextContainer = null;

                usedTexture = currentTexture;
            }
        }

        private void RenderFromWindow(Window outputWindow, TextureHandle outputTexture)
        {
            using (new ProfilingScope(outputWindow.WindowTitle))
            {
                SetupContextForWindow(outputWindow, ref outputTexture, out RenderContextContainer context);

                foreach (IRenderStage stage in _renderStages)
                {
                    stage.FlushPassesForWindow(this, _frameGraph.Blackboard, context);
                }

                PushRenderPass(_presentPass);

                _currentContextContainer = null;
            }
        }

        /// <summary>Not thread-safe</summary>
        public void RenderFromCamera(SceneEntity entity)
        {
            Guard.IsFalse(entity.IsNull);

            throw new NotImplementedException();
        }

        private void SetupContextForOutput(RenderOutputData outputData, out RenderContextContainer context)
        {
            context = _renderContextPool.GetCameraContext();
            _currentContextContainer = context;

            RenderStateData stateData = context.GetOrCreate(static () => new RenderStateData());
            {
                stateData.SwapChain = _swapChainCache.GetForWindow(outputData.Window!);
            }

            RenderCameraData cameraData = context.GetOrCreate(static () => new RenderCameraData());
            {
                cameraData.Setup(outputData);

                if (cameraData.OutputData.TargetTexture == null)
                {
                    cameraData.ColorTexture = _frameGraph.ResourceManager.CreateTexture(new FGTextureDesc(cameraData.ClientSize.X, cameraData.ClientSize.Y, RHIFormat.RGB10A2_UNorm, FGTextureTarget.RenderTarget, "CamColor"));
                }
                else
                {
                    cameraData.ColorTexture = _frameGraph.ResourceManager.ImportTexture(cameraData.OutputData.TargetTexture);
                }

                cameraData.DepthTexture = _frameGraph.ResourceManager.CreateTexture(new FGTextureDesc(cameraData.ClientSize.X, cameraData.ClientSize.Y, RHIFormat.D24_UNorm_S8_UInt, FGTextureTarget.DepthStencil, "CamDepth"));
            }

            //PushRenderPass(_setupCameraPass);
        }

        private void SetupContextForWindow(Window window, ref TextureHandle outputTexture, out RenderContextContainer context)
        {
            context = _renderContextPool.GetWindowContext();
            _currentContextContainer = context;

            RenderStateData stateData = context.GetOrCreate(static () => new RenderStateData());
            {
                stateData.SwapChain = _swapChainCache.GetForWindow(window);
            }

            RenderWindowData windowData = context.GetOrCreate(static () => new RenderWindowData());
            {
                windowData.Window = window;
                windowData.ColorTexture = outputTexture;

                if (windowData.ColorTexture.IsNull)
                {
                    windowData.ColorTexture = _frameGraph.ResourceManager.CreateTexture(new FGTextureDesc(windowData.Window.ClientSize.X, windowData.Window.ClientSize.Y, RHIFormat.RGB10A2_UNorm, FGTextureTarget.RenderTarget, "WndColor"));
                }
            }

            // PushRenderPass(_setupWindowPass);
        }

        public void PushRenderPass<T>(T renderPass) where T : IRenderPass
        {
            ArgumentNullException.ThrowIfNull(renderPass);
            Debug.Assert(_currentContextContainer != null);
            _frameGraph.PushRenderPass(renderPass, _currentContextContainer!);
        }

        public JobHandle RequestBatch(RenderList renderList, in BatchingOptions options)
        {
            _batchingManager.BatchWorld(renderList, new BatchWorldSetup(_octreeManager, options.ViewFrustrum.GetValueOrDefault()));
            return JobHandle.Completed;
        }

        public void InspectRenderPlan(Action<CompiledRenderPlan> action) => _inspectRenderPlan.Subscribe(action);

        public RHIDevice GraphicsDevice => _graphicsDevice;
        public RHISampler DefaultSampler => _defaultSampler;

        internal IPlaybackDevice PlaybackDevice => _playbackDevice;

        public FrameGraph FrameGraph => _frameGraph;
        public OctreeManager OctreeManager => _octreeManager;
        public RenderWorld RenderWorld => _renderWorld;
        public BatchingManager BatchingManager => _batchingManager;
        public DebugManager DebugManager => _debugManager;
        public ShaderGlobalsManager GlobalsManager => _globalsManager;
        public SwapChainCache SwapChainCache => _swapChainCache;
        public RenderPrimitives RenderPrimitives => _renderPrimitives;

        public ROList<IRenderStage> RenderStages => _renderStages;

        ///<summary>Not thread-safe</summary>
        public Action? SubmitTransitionalPasses;
    }
}
