using Primary.Profiling;
using Primary.Rendering2.Assets;
using Primary.Rendering2.Batching;
using Primary.Rendering2.Data;
using Primary.Rendering2.Debuggable;
using Primary.Rendering2.Resources;
using Primary.Rendering2.Tree;
using System.Diagnostics;

namespace Primary.Rendering2
{
    public class RenderingManager
    {
        private OctreeManager _octreeManager;
        private RenderWorld _renderWorld;
        private RenderContextContainer _contextContainer;
        private RenderPassManager _renderPassManager;
        private BatchingManager _batchingManager;
        private ShaderGlobalsManager _globalsManager;

        private IRenderPath? _currentPath;

        public RenderingManager()
        {
            _octreeManager = new OctreeManager();
            _renderWorld = new RenderWorld();
            _contextContainer = new RenderContextContainer();
            _renderPassManager = new RenderPassManager();
            _batchingManager = new BatchingManager();
            _globalsManager = new ShaderGlobalsManager();

            _currentPath = null;
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

                            _renderPassManager.SetupPasses(_contextContainer);
                            _renderPassManager.CompilePasses(_contextContainer);
                            _renderPassManager.ExecutePasses(_contextContainer);
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
                    cameraData.ColorTexture = desc.CreateTexture(new FrameGraphTextureDesc(baseDesc) { Format = FGTextureFormat.RGB10A2_UNorm, Usage = FGTextureUsage.RenderTarget });
                    cameraData.DepthTexture = desc.CreateTexture(new FrameGraphTextureDesc(baseDesc) { Format = FGTextureFormat.D24_UNorm_S8_UInt, Usage = FGTextureUsage.DepthStencil });
                }
            }
        }

        public void RenderDebug(IDebugRenderer renderer)
        {
            OctreeVisualizer.Visualize(_octreeManager, renderer);
        }

        public OctreeManager OctreeManager => _octreeManager;
        //public RenderWorld RenderWorld => _renderWorld;
        //public RenderContextContainer ContextContainer => _contextContainer;
        public RenderPassManager RenderPassManager => _renderPassManager;
        public BatchingManager BatchingManager => _batchingManager;
    }
}
