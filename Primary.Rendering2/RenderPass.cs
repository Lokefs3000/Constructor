using CommunityToolkit.HighPerformance;
using Primary.Rendering2.Pass;
using Primary.Rendering2.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2
{
    public sealed class RenderPass
    {
        private RenderPassErrorReporter _errorReporter;
        private RenderPassBlackboard _blackboard;

        private Dictionary<Type, IPassData> _passDataPool;
        private List<RenderPassDescription> _passes;

        private int _resourceCounter;

        internal RenderPass()
        {
            _errorReporter = new RenderPassErrorReporter();
            _blackboard = new RenderPassBlackboard();

            _passDataPool = new Dictionary<Type, IPassData>();
            _passes = new List<RenderPassDescription>();
        }

        internal int GetNewResourceIndex() => _resourceCounter++;

        internal void ClearInternals()
        {
            _passes.Clear();

            _resourceCounter = 0;
        }

        public RasterPassDescription SetupRasterPass<T>(string name, out T data) where T : class, IPassData, new()
        {
            if (!_passDataPool.TryGetValue(typeof(T), out IPassData? passData))
            {
                passData = new T();
                _passDataPool[typeof(T)] = passData;
            }

            data = Unsafe.As<T>(passData);
            data.Clear();

            RasterPassDescription desc = new RasterPassDescription(this, name);
            return desc;
        }

        internal void AddNewRenderPass(RenderPassDescription desc) => _passes.Add(desc);

        internal void ReportError(RPErrorSource source, RPErrorType type) => _errorReporter.ReportError(source, type);

        public RenderPassBlackboard Blackboard => _blackboard;

        internal ReadOnlySpan<RenderPassDescription> Passes => _passes.AsSpan();
    }
}
