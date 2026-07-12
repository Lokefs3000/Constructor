using CommunityToolkit.Diagnostics;
using Primary.Assets;
using Primary.Common;
using Primary.Components;
using Primary.Profiling;
using Primary.Rendering.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Debuggable;
using Primary.Rendering.NRD;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Statistics;
using Primary.Rendering.Structures;
using Primary.Rendering.Tree;
using Primary.RHI;
using Primary.Scenes;
using Primary.Windowing;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Primary.Rendering
{
    public class RenderingManager : IDisposable
    {
        private readonly RHIDevice _graphicsDevice;

        private readonly DebugManager _debugManager;
        private readonly OctreeManager _octreeManager;
        private readonly RenderWorld _renderWorld;
        private readonly RenderContextPool _renderContextPool;
        private readonly RenderPassManager _renderPassManager;
        private readonly BatchingManager _batchingManager;
        private readonly ShaderGlobalsManager _globalsManager;
        private readonly SwapChainCache _swapChainCache;
        private readonly INativeRenderDispatcher _nrdDevice;
        private readonly RenderPrimitives _renderPrimitives;
        private readonly RenderStatistics _statistics;

        private IRenderPath? _currentPath;

        private ShaderAsset _finalBlitSwapChain;
        private PropertyBlock _finalBlitSwapChainPB;

        private HashSet<Window> _renderedWindows;

        private bool _disposedValue;

        public RenderingManager()
        {
            _graphicsDevice = RHIDeviceFactory.CreateDefaultApi(new RHIDeviceDescription { EnableValidation = Engine.IsDebugBuild && !AppArguments.HasArgument("rhi-nodebug") }, EngLog.RHI);

            _debugManager = new DebugManager();
            _octreeManager = new OctreeManager();
            _renderWorld = new RenderWorld();
            _renderContextPool = new RenderContextPool();
            _renderPassManager = new RenderPassManager(this);
            _batchingManager = new BatchingManager(this);
            _globalsManager = new ShaderGlobalsManager();
            _swapChainCache = new SwapChainCache(this);
            _nrdDevice = NRDFactory.Create(this, _graphicsDevice);
            _renderPrimitives = new RenderPrimitives();
            _statistics = new RenderStatistics();

            _currentPath = null;

            _finalBlitSwapChain = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/Core/BlitToSwapChain.shader").WaitIfNotLoaded();
            _finalBlitSwapChainPB = new PropertyBlock(_finalBlitSwapChain);

            _renderedWindows = new HashSet<Window>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _currentPath?.Uninstall(this);
                    _currentPath = null;

                    _finalBlitSwapChainPB.Dispose();

                    _renderPrimitives.Dispose();
                    _nrdDevice.Dispose();
                    _swapChainCache.Dispose();
                    _renderPassManager.Dispose();

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
        public void SetNewRenderPath(IRenderPath path)
        {
            _currentPath?.Uninstall(this);
            path.Install(this);

            _currentPath = path;
        }

        public void Render()
        {
            using (new ProfilingScope("Render"))
            {
                using (new ProfilingScope("Perform"))
                {
                    _renderedWindows.Clear();

                    _statistics.ResetTransientStats();

                    _graphicsDevice.IncrementFrame();

                    _renderContextPool.ResetPoolForNewRender();
                    _octreeManager.QueryPending();
                    _renderWorld.SetupWorld();

                    if (_currentPath != null)
                    {
                        for (int i = 0; i < _renderWorld.Outputs.Length; i++)
                        {
                            RenderOutputData outputData = _renderWorld.Outputs[i];

                            FrameGraphTexture currentTexture = FrameGraphTexture.Invalid;
                            if (_currentPath != null)
                            {
                                RenderFromPerspective(outputData, out currentTexture);
                            }

                            if (i + 1 == _renderWorld.Outputs.Length || _renderWorld.Outputs[i + 1].Window != outputData.Window)
                            {
                                if (currentTexture.IsExternal)
                                    currentTexture = FrameGraphTexture.Invalid;

                                RenderFromWindow(outputData.Window, currentTexture);

                                _renderedWindows.Add(outputData.Window);
                            }
                        }

                        foreach (var kvp in WindowManager.Instance.Windows)
                        {
                            Window window = kvp.Value;
                            if (window.IsShown && _renderedWindows.Add(window))
                            {
                                RenderFromWindow(window, FrameGraphTexture.Invalid);
                            }
                        }

                        if (!_renderPassManager.RenderPass.Passes.IsEmpty)
                        {
                            _renderPassManager.CompilePasses();
                            _renderPassManager.ExecutePasses();
                        }
                    }
                }

                if (!_renderPassManager.Commands.IsEmpty)
                {
                    using (new ProfilingScope("Dispatch"))
                    {
                        _nrdDevice.Dispatch(_renderPassManager);
                    }
                }

                _renderPassManager.ClearInternals();
                _globalsManager.CleanupTransitional();

                _renderedWindows.Clear();
            }
        }

        private void RenderFromPerspective(RenderOutputData outputData, out FrameGraphTexture usedTexture)
        {
            Guard.IsNotNull(_currentPath);

            using (new ProfilingScope(Engine.IsDebugBuild ? $"Cam-{outputData.Entity.Name}" : "Cam-Default"))
            {
                usedTexture = FrameGraphTexture.Invalid;

                int previousPassCount = _renderPassManager.RenderPass.Passes.Length;

                SetupContextForOutput(outputData, out RenderContextContainer context);

                _currentPath?.PreRenderPassSetup(this);
                _renderPassManager.SetupPasses(RenderPassRunContext.PerCamera, context);

                if (_renderPassManager.RenderPass.Passes.Length > previousPassCount)
                {
                    FrameGraphTexture currentTexture = context.Get<RenderCameraData>()!.ColorTexture;
                    if (!currentTexture.IsExternal)
                        SetupPresentForOutput(outputData.Window, currentTexture);

                    usedTexture = currentTexture;
                    _renderPassManager.PushCurrentPassGroup(usedTexture, RenderPassRunContext.PerCamera, context);
                }
                else
                    _renderPassManager.ClearCurrentPassGroup();

                //_renderPassManager.ClearLocalData();
            }
        }

        private void RenderFromWindow(Window outputWindow, FrameGraphTexture outputTexture)
        {
            Guard.IsNotNull(_currentPath);

            using (new ProfilingScope(outputWindow.WindowTitle))
            {
                int previousPassCount = _renderPassManager.RenderPass.Passes.Length;

                SetupContextForWindow(outputWindow, ref outputTexture, out RenderContextContainer context);

                _renderPassManager.SetupPasses(RenderPassRunContext.PerWindow, context);

                if (_renderPassManager.RenderPass.Passes.Length > previousPassCount)
                {
                    FrameGraphTexture currentTexture = context.Get<RenderWindowData>()!.ColorTexture;
                    SetupPresentForOutput(outputWindow, currentTexture);

                    _renderPassManager.PushCurrentPassGroup(currentTexture, RenderPassRunContext.PerWindow, context);
                }
                else
                    _renderPassManager.ClearCurrentPassGroup();

                //_renderPassManager.ClearLocalData();
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
            RenderPass renderPass = _renderPassManager.RenderPass;
            using (RasterPassDescription desc = renderPass.SetupRasterPass(string.Empty, out GenericPassData _))
            {
                context = _renderContextPool.GetCameraContext();

                {
                    RenderStateData stateData = context.GetOrCreate(() => new RenderStateData());
                    stateData.Path = _currentPath!;
                }
                {
                    RenderCameraData cameraData = context.GetOrCreate(() => new RenderCameraData());

                    RHISwapChain swapChain = _swapChainCache.GetForWindow(outputData.Window!, true)!;
                    //TODO: Check conditional first before trying to resize the swapchain
                    //TODO: Get the NRD to wait on gpu work before resizing
                    //swapChain.ResizeBuffersToNewSize();

                    FrameGraphTextureDesc baseDesc = new FrameGraphTextureDesc
                    {
                        Width = Math.Min((int)outputData.ProjectionData.ClientSize.X, (int)swapChain.Description.WindowSize.X),
                        Height = Math.Min((int)outputData.ProjectionData.ClientSize.Y, (int)swapChain.Description.WindowSize.Y),
                    };

                    if (outputData.TargetTexture != null && Flags.HasFlag(outputData.TargetTexture.Description.Usage, RHIResourceUsage.RenderTarget))
                        cameraData.ColorTexture = outputData.TargetTexture;
                    else
                        cameraData.ColorTexture = desc.CreateTexture(new FrameGraphTextureDesc(baseDesc) { Format = RHIFormat.RGB10A2_UNorm, Usage = FGTextureUsage.RenderTarget | FGTextureUsage.ShaderResource | FGTextureUsage.PixelShader }, "CamColor");

                    cameraData.DepthTexture = desc.CreateTexture(new FrameGraphTextureDesc(baseDesc) { Format = RHIFormat.D24_UNorm_S8_UInt, Usage = FGTextureUsage.DepthStencil | FGTextureUsage.ShaderResource | FGTextureUsage.PixelShader }, "CamDepth");

                    cameraData.Setup(outputData);
                }
            }
        }

        private void SetupContextForWindow(Window window, ref FrameGraphTexture outputTexture, out RenderContextContainer context)
        {
            RenderPass renderPass = _renderPassManager.RenderPass;
            using (RasterPassDescription desc = renderPass.SetupRasterPass(string.Empty, out GenericPassData _))
            {
                context = _renderContextPool.GetWindowContext();

                {
                    RenderStateData stateData = context.GetOrCreate(() => new RenderStateData());
                    stateData.Path = _currentPath!;
                }

                {
                    RenderWindowData windowData = context.GetOrCreate(() => new RenderWindowData());
                    RHISwapChain swapChain = _swapChainCache.GetForWindow(window, true)!;

                    FrameGraphTextureDesc baseDesc = new FrameGraphTextureDesc
                    {
                        Width = window.ClientSize.X,
                        Height = window.ClientSize.Y,
                    };

                    if (outputTexture.IsNull)
                    {
                        outputTexture = desc.CreateTexture(new FrameGraphTextureDesc(baseDesc) { Format = RHIFormat.RGB10A2_UNorm, Usage = FGTextureUsage.RenderTarget | FGTextureUsage.ShaderResource | FGTextureUsage.PixelShader }, "WndColor");
                    }

                    windowData.Window = window;
                    windowData.ColorTexture = outputTexture;
                }
            }
        }

        private void SetupPresentForOutput(Window window, FrameGraphTexture outputTexture)
        {
            RenderPass renderPass = _renderPassManager.RenderPass;
            using (RasterPassDescription desc = renderPass.SetupRasterPass("CorePresent", out PresentForOutputData passData))
            {
                {
                    passData.Shader = _finalBlitSwapChain;
                    passData.Block = _finalBlitSwapChainPB;

                    passData.PresentWindow = window;
                    passData.Texture = outputTexture;
                }

                desc.UseResource(FGResourceUsage.Read, outputTexture);
                desc.AllowPassCulling(false);

                desc.SetRenderFunction<PresentForOutputData>(PassFunction);
            }

            static void PassFunction(RasterPassContext context, PresentForOutputData passData)
            {
                RasterCommandBuffer cmd = context.CommandBuffer;

                passData.Block!.SetResource("txFinalTexture", passData.Texture);

                cmd.PresentOnWindow(passData.PresentWindow!);

                cmd.SetViewport(0, new FGViewport(0, 0, passData.Texture.Description.Width, passData.Texture.Description.Height));
                cmd.SetScissor(0, new FGRect(0, 0, passData.Texture.Description.Width, passData.Texture.Description.Height));

                cmd.SetPipeline(passData.Shader!);
                cmd.SetProperties(passData.Block!);

                cmd.DrawInstanced(new FGDrawInstancedDesc(3));
            }
        }

        private sealed class PresentForOutputData : IPassData
        {
            public ShaderAsset? Shader;
            public PropertyBlock? Block;

            public Window? PresentWindow;
            public FrameGraphTexture Texture;
            public FrameGraphTexture Source;

            public void Clear()
            {
                Shader = null;
                Block = null;

                PresentWindow = null;
                Texture = FrameGraphTexture.Invalid;
                Source = FrameGraphTexture.Invalid;
            }
        }

        public void RenderDebug(Debuggable.IDebugRenderer renderer)
        {
            OctreeVisualizer.Visualize(_octreeManager, renderer);
        }

        public RHIDevice GraphicsDevice => _graphicsDevice;

        public OctreeManager OctreeManager => _octreeManager;
        public RenderWorld RenderWorld => _renderWorld;
        public RenderPassManager RenderPassManager => _renderPassManager;
        public BatchingManager BatchingManager => _batchingManager;
        public ShaderGlobalsManager GlobalsManager => _globalsManager;
        public SwapChainCache SwapChainCache => _swapChainCache;
        public INativeRenderDispatcher NRDDevice => _nrdDevice;
        public RenderPrimitives RenderPrimitives => _renderPrimitives;
        public RenderStatistics Statistics => _statistics;

        public IRenderPath? CurrentRenderPath => _currentPath;

        ///<summary>Not thread-safe</summary>
        public Action? SubmitTransitionalPasses;
    }
}
