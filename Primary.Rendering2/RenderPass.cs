using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Rendering2.Assets;
using Primary.Rendering2.Pass;
using Primary.Rendering2.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2
{
    public sealed class RenderPass
    {
        private readonly RenderPassManager _manager;

        private RenderPassErrorReporter _errorReporter;
        private RenderPassBlackboard _blackboard;

        private Dictionary<Type, IPassData> _passDataPool;
        private List<RenderPassDescription> _passes;

        private int _resourceCounter;

        internal RenderPass(RenderPassManager manager)
        {
            _manager = manager;

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

        internal IPassData? GetPassData(Type type)
        {
            if (_passDataPool.TryGetValue(type, out IPassData? data))
                return data;
            return null;
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

        internal void ReportError(RPErrorSource source, RPErrorType type, string? resourceName) => _errorReporter.ReportError(source, type, resourceName);

        public RenderPassManager Manager => _manager;
        public RenderPassBlackboard Blackboard => _blackboard;

        internal ReadOnlySpan<RenderPassDescription> Passes => _passes.AsSpan();

        internal static void AddGlobalResources(PooledList<UsedResourceData> resources, PooledList<UsedRenderTargetData> renderTargets)
        {
            _globalResourceHash.Clear();

            foreach (UsedResourceData data in resources)
            {
                Debug.Assert(data.Resource.IsValidAndRenderGraph);
                _globalResourceHash.Add(data.Resource.Index);
            }

            foreach (UsedRenderTargetData data in renderTargets)
            {
                Debug.Assert(((FrameGraphResource)data.Target).IsValidAndRenderGraph);
                _globalResourceHash.Add(data.Target.Index);
            }

            ShaderGlobalsManager instance = ShaderGlobalsManager.Instance;
            foreach (var kvp in instance.TransitionalProperties)
            {
                if (!_globalResourceHash.Contains(kvp.Key) && instance.TryGetPropertyValue(kvp.Value.String, out PropertyData data))
                    resources.Add(new UsedResourceData(FGResourceUsage.Read, data.Resource));
            }
        }

        [ThreadStatic]
        private static HashSet<int> _globalResourceHash = new HashSet<int>();
    }
}
