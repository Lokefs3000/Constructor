using Primary.Profiling;
using Primary.Rendering2.Pass;
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
        private RenderPassCompiler _renderPassCompiler;

        private List<IRenderPass> _activePasses;

        internal RenderPassManager()
        {
            _renderPass = new RenderPass();
            _renderPassCompiler = new RenderPassCompiler();

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

        internal void CompilePasses(RenderContextContainer contextContainer)
        {
            using (new ProfilingScope("Compile"))
            {
                _renderPassCompiler.Compile(_renderPass.Passes);
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
