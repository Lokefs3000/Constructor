using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Profiling;
using Primary.Rendering;
using Primary.Rendering2.Assets;
using Primary.Rendering2.Batching;
using Primary.Rendering2.Data;
using Primary.Rendering2.Debuggable;
using Primary.Rendering2.NRD;
using Primary.Rendering2.Recording;
using Primary.Rendering2.Resources;
using Primary.Rendering2.Structures;
using Primary.Rendering2.Tree;
using Primary.RHI2;
using System.Diagnostics;

namespace Primary.Rendering2
{
    public class RenderingManager
    {
        private readonly RHIDevice _graphicsDevice;

        private readonly OctreeManager _octreeManager;
        private readonly RenderWorld _renderWorld;
        private readonly RenderContextContainer _contextContainer;
        private readonly RenderPassManager _renderPassManager;
        private readonly BatchingManager _batchingManager;
        private readonly ShaderGlobalsManager _globalsManager;
        private readonly SwapChainCache _swapChainCache;
        private readonly INativeRenderDispatcher _nrdDevice;

        private IRenderPath? _currentPath;

        private ShaderAsset2 _finalBlitSwapChain;
        private PropertyBlock _finalBlitSwapChainPB;

        public RenderingManager()
        {
            _graphicsDevice = RHIDeviceFactory.CreateDefaultApi(new RHIDeviceDescription { EnableValidation = Engine.IsDebugBuild }, EngLog.RHI);

            _octreeManager = new OctreeManager();
            _renderWorld = new RenderWorld();
            _contextContainer = new RenderContextContainer();
            _renderPassManager = new RenderPassManager(this);
            _batchingManager = new BatchingManager();
            _globalsManager = new ShaderGlobalsManager();
            _swapChainCache = new SwapChainCache(this);
            _nrdDevice = NRDFactory.Create(this, _graphicsDevice);

            _currentPath = null;

            _finalBlitSwapChain = AssetManager.LoadAsset<ShaderAsset2>("Engine/Shaders/FinalBlitSwapChain.hlsl2", true);
            _finalBlitSwapChainPB = new PropertyBlock(_finalBlitSwapChain);
        }

        /// <summary>Not thread-safe</summary>
        public void SetNewRenderPath(IRenderPath path)
        {
            _currentPath?.Uinstall(this);
            path.Install(this);

            _currentPath = path;
        }

        public void Render()
        {
            using (new ProfilingScope("Render2"))
            {
                _octreeManager.QueryPending();
                _renderWorld.SetupWorld();

                if (_currentPath != null)
                {
                    SetupGlobalContext();

                    foreach (RenderOutputData outputData in _renderWorld.Outputs)
                    {
                        using (new ProfilingScope(Engine.IsDebugBuild ? $"Cam-{outputData.Entity.Name}" : "Cam-Default"))
                        {
                            _renderPassManager.ClearInternals();

                            SetupContextForOutput(outputData);
                            _currentPath?.PreRenderPassSetup(this);
                            SubmitTransitionalPasses?.Invoke();

                            _renderPassManager.SetWindowOutput(
                                _swapChainCache.GetForWindow(outputData.Window),
                                _contextContainer.Get<RenderCameraData>()!.ColorTexture);

                            _renderPassManager.SetupPasses(_contextContainer);
                            SetupPresentForOutput(outputData);

                            _renderPassManager.CompilePasses(_contextContainer);
                            _renderPassManager.ExecutePasses(_contextContainer);

                            using (new ProfilingScope("NRD-Dispatch"))
                            {
                                _nrdDevice.Dispatch(_renderPassManager);
                            }
                        }
                    }
                }

                _globalsManager.CleanupTransitional();
            }
        }

        private void SetupGlobalContext()
        {
            Debug.Assert(_currentPath != null);

            RenderStateData stateData = _contextContainer.GetOrCreate<RenderStateData>(static () => new RenderStateData());
            stateData.Path = _currentPath;
        }

        private void SetupContextForOutput(RenderOutputData outputData)
        {
            RenderPass renderPass = _renderPassManager.RenderPass;
            using (RasterPassDescription desc = renderPass.SetupRasterPass(string.Empty, out GenericPassData _))
            {
                {
                    RenderStateData stateData = _contextContainer.GetOrCreate(() => new RenderStateData());
                    stateData.Path = _currentPath!;
                }
                {
                    RenderCameraData cameraData = _contextContainer.GetOrCreate(() => new RenderCameraData());

                    FrameGraphTextureDesc baseDesc = new FrameGraphTextureDesc
                    {
                        Width = (int)outputData.ProjectionData.ClientSize.X,
                        Height = (int)outputData.ProjectionData.ClientSize.Y,
                    };

                    cameraData.CameraEntity = outputData.Entity;
                    cameraData.ColorTexture = desc.CreateTexture(new FrameGraphTextureDesc(baseDesc) { Format = FGTextureFormat.RGB10A2_UNorm, Usage = FGTextureUsage.RenderTarget | FGTextureUsage.ShaderResource | FGTextureUsage.PixelShader }, "CamColor");
                    cameraData.DepthTexture = desc.CreateTexture(new FrameGraphTextureDesc(baseDesc) { Format = FGTextureFormat.D24_UNorm_S8_UInt, Usage = FGTextureUsage.DepthStencil }, "CamDepth");

                    cameraData.Setup(outputData);
                }
            }
        }

        private void SetupPresentForOutput(RenderOutputData outputData)
        {
            RenderPass renderPass = _renderPassManager.RenderPass;
            using (RasterPassDescription desc = renderPass.SetupRasterPass("CorePresent", out PresentForOutputData passData))
            {
                RenderCameraData cameraData = _contextContainer.Get<RenderCameraData>()!;

                FrameGraphTexture source = desc.CreateTexture(new FrameGraphTextureDesc(cameraData.ColorTexture.Description)
                {
                    Format = FGTextureFormat.RGBA8_UNorm,
                    Usage = FGTextureUsage.RenderTarget
                }, "Source");

                {
                    passData.Shader = _finalBlitSwapChain;
                    passData.Block = _finalBlitSwapChainPB;

                    passData.PresentWindow = outputData.Window;
                    passData.Texture = cameraData.ColorTexture;
                    passData.Source = source;
                }

                desc.UseResource(FGResourceUsage.Read, cameraData.ColorTexture);
                desc.UseResource(FGResourceUsage.Read | FGResourceUsage.NoShaderAccess, source);

                desc.UseRenderTarget(source);
                desc.AllowPassCulling(false);

                desc.SetRenderFunction<PresentForOutputData>(PassFunction);
            }

            static void PassFunction(RasterPassContext context, PresentForOutputData passData)
            {
                RasterCommandBuffer cmd = context.CommandBuffer;

                //blit compatible format
                {
                    passData.Block!.SetResource(PropertyBlock.GetID("txFinalTexture"), passData.Texture);
       
                    cmd.SetRenderTarget(0, passData.Source);
                    cmd.SetPipeline(passData.Shader!.GraphicsPipeline!);
                    cmd.SetProperties(passData.Block!);
                    cmd.DrawInstanced(new FGDrawInstancedDesc(3));
                }

                cmd.PresentOnWindow(passData.PresentWindow!, passData.Source);
            }
        }

        private sealed class PresentForOutputData : IPassData
        {
            public ShaderAsset2? Shader;
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
        public RenderContextContainer ContextContainer => _contextContainer;
        public RenderPassManager RenderPassManager => _renderPassManager;
        public BatchingManager BatchingManager => _batchingManager;
        public ShaderGlobalsManager GlobalsManager => _globalsManager;
        public SwapChainCache SwapChainCache => _swapChainCache;
        public INativeRenderDispatcher NRDDevice => _nrdDevice;

        public IRenderPath? CurrentRenderPath => _currentPath;

        ///<summary>Not thread-safe</summary>
        public Action? SubmitTransitionalPasses;
    }
}
