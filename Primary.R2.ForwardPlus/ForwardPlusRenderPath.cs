using Primary.R2.ForwardPlus.Passes;
using Primary.Rendering2;
using Primary.Rendering2.Batching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.R2.ForwardPlus
{
    public sealed class ForwardPlusRenderPath : IRenderPath
    {
        private bool _isInstalled;

        private RenderList? _primaryRenderList;

        public void PreRenderPassSetup(RenderingManager manager)
        {
            Debug.Assert(_isInstalled);

            manager.BatchingManager.BatchWorld(manager.OctreeManager, _primaryRenderList!);
        }

        public void Install(RenderingManager manager)
        {
            Debug.Assert(!_isInstalled);
            _isInstalled = true;

            _primaryRenderList = manager.BatchingManager.CreateRenderList();

            manager.RenderPassManager.AddRenderPass<ResourcesPass>();
            manager.RenderPassManager.AddRenderPass<DepthPrePass>();
            manager.RenderPassManager.AddRenderPass<OpaquePass>();
        }

        public void Uinstall(RenderingManager manager)
        {
            Debug.Assert(_isInstalled);
            _isInstalled = false;

            _primaryRenderList!.Dispose();
            _primaryRenderList = null;

            manager.RenderPassManager.RemoveRenderPass<OpaquePass>();
            manager.RenderPassManager.RemoveRenderPass<DepthPrePass>();
            manager.RenderPassManager.RemoveRenderPass<ResourcesPass>();
        }

        internal RenderList? PrimaryRenderList => _primaryRenderList;
    }
}
