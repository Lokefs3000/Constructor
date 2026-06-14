using Primary.Assets;
using Primary.R2.ForwardPlus.Passes;
using Primary.R2.ForwardPlus.Statistics;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Batching;
using System.Diagnostics;

namespace Primary.R2.ForwardPlus
{
    public sealed class ForwardPlusRenderPath : IRenderPath
    {
        private bool _isInstalled;

        private RenderPathStatistics _statistics;
        private RenderList? _primaryRenderList;

        public ForwardPlusRenderPath()
        {
            _isInstalled = false;

            _statistics = new RenderPathStatistics();
            _primaryRenderList = null;
        }

        public void PreRenderPassSetup(RenderingManager manager)
        {
            Debug.Assert(_isInstalled);

            _statistics.ClearTransientData();

            manager.BatchingManager.BatchWorld(manager.OctreeManager, _primaryRenderList!);
        }

        public void Install(RenderingManager manager)
        {
            Debug.Assert(!_isInstalled);
            _isInstalled = true;

            _primaryRenderList = manager.BatchingManager.CreateRenderList();
            _primaryRenderList.DefaultMaterial = AssetManager.LoadAsset<MaterialAsset>("Engine/Materials/ForwardPlus/MissingDefMat.mat", true);

            manager.RenderPassManager.AddRenderPass<ResourcesPass>();
            manager.RenderPassManager.AddRenderPass<DepthPrePass>();
            manager.RenderPassManager.AddRenderPass<OpaquePass>();

            //manager.RenderPassManager.AddRenderPass<TestPass>();
        }

        public void Uninstall(RenderingManager manager)
        {
            Debug.Assert(_isInstalled);
            _isInstalled = false;

            _primaryRenderList!.Dispose();
            _primaryRenderList = null;

            manager.RenderPassManager.RemoveRenderPass<OpaquePass>();
            manager.RenderPassManager.RemoveRenderPass<DepthPrePass>();
            manager.RenderPassManager.RemoveRenderPass<ResourcesPass>();
        }

        public RenderPathStatistics Statistics => _statistics;
        internal RenderList? PrimaryRenderList => _primaryRenderList;
    }
}
