using Primary.Profiling;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2
{
    public sealed class RenderPassManager
    {
        private RenderPass _renderPass;

        private List<IRenderPass> _activePasses;

        internal RenderPassManager()
        {
            _renderPass = new RenderPass();

            _activePasses = new List<IRenderPass>();
        }

        internal void SetupPasses(RenderContextContainer contextContainer)
        {
            using (new ProfilingScope("Setup"))
            {
                _renderPass.ClearInternals();
                foreach (IRenderPass renderPass in _activePasses)
                {
                    renderPass.SetupRenderPasses(_renderPass, contextContainer);
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
    }
}
