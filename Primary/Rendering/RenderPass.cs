using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Common;
using Primary.Rendering.Assets;
using Primary.Rendering.Pass;
using Primary.Rendering.Resources;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Primary.Rendering
{
    public class RenderPass
    {
        private readonly RenderPassManager _manager;

        private RenderPassErrorReporter _errorReporter;
        private RenderPassBlackboard _blackboard;

        private PassDataStorage _passDataStorage;
        private PassArrayStorage _passArrayStorage;

        private List<RenderPassDescription> _passes;
        private List<RenderPassGroup> _groups;

        private int _resourceCounter;
        private int _groupCounter;

        internal RenderPass(RenderPassManager manager)
        {
            _manager = manager;

            _errorReporter = new RenderPassErrorReporter();
            _blackboard = new RenderPassBlackboard();

            _passDataStorage = new PassDataStorage();
            _passArrayStorage = new PassArrayStorage();

            _passes = new List<RenderPassDescription>();
            _groups = new List<RenderPassGroup>();

            _resourceCounter = 0;
            _groupCounter = 0;
        }

        internal int GetNewResourceIndex() => _resourceCounter++;

        internal void ClearInternals()
        {
            _passDataStorage.ClearEntries(true);
            _passArrayStorage.ClearArrays();
            _blackboard.EraseBlackboards();

            _passes.Clear();
            _groups.Clear();

            _resourceCounter = 0;
            _groupCounter = 0;
        }

        internal void ClearLocalData()
        {
            _passDataStorage.ClearEntries(false);
            _passArrayStorage.ClearArrays();
            _blackboard.EraseBlackboards();

            _passes.Clear();
        }

        public RasterPassDescription SetupRasterPass<T>(string name, out T data) where T : class, IPassData, new()
        {
            data = _passDataStorage.GetPassData<T>();

            RasterPassDescription desc = new RasterPassDescription(this, name, data, new PassArray(_passArrayStorage));
            return desc;
        }

        public ComputePassDescription SetupComputePass<T>(string name, out T data) where T : class, IPassData, new()
        {
            data = _passDataStorage.GetPassData<T>();

            ComputePassDescription desc = new ComputePassDescription(this, name, data, new PassArray(_passArrayStorage));
            return desc;
        }

        internal void AddNewRenderPass(RenderPassDescription desc) => _passes.Add(desc);

        internal void PushCurrentPassGroup(FrameGraphTexture finalTexture, RenderPassRunContext runContext, RenderContextContainer context)
        {
            if (_groups.Count == 0)
            {
                if (_passes.Count > 0)
                    _groups.Add(new RenderPassGroup(new IndexRange(0, _passes.Count), finalTexture, runContext, context));
            }
            else
            {
                RenderPassGroup lastGroup = _groups[^1];
                if (lastGroup.Range.End < _passes.Count)
                    _groups.Add(new RenderPassGroup(new IndexRange(lastGroup.Range.End, _passes.Count), finalTexture, runContext, context));
            }
        }

        internal void ClearCurrentPassGroup()
        {
            if (_groups.Count == 0)
            {
                _passes.Clear();
            }
            else
            {
                RenderPassGroup lastGroup = _groups[^1];
                if (lastGroup.Range.End < _passes.Count)
                    _passes.RemoveRange(lastGroup.Range.End, _passes.Count - lastGroup.Range.End);
            }
        }

        internal void ReportError(RPErrorSource source, RPErrorType type, string? resourceName) => _errorReporter.ReportError(source, type, resourceName);

        public RenderPassManager Manager => _manager;
        public RenderPassBlackboard Blackboard => _blackboard;

        internal ReadOnlySpan<RenderPassDescription> Passes => _passes.AsSpan();
        internal ReadOnlySpan<RenderPassGroup> Groups => _groups.AsSpan();

        internal int CurrentGroupIndex => _groups.Count;

        internal static void AddGlobalResources(ref PassArray array)
        {
            foreach (UsedResourceData data in array.UsedResources)
            {
                t_globalResourceHash.Add(data.Resource);
            }

            foreach (UsedRenderTargetData data in array.UsedRenderTargets)
            {
                t_globalResourceHash.Add(data.Target);
            }

            ShaderGlobalsManager instance = ShaderGlobalsManager.Instance;
            foreach (var kvp in instance.GlobalProperties)
            {
                if (kvp.Value.Aux == null && !t_globalResourceHash.Contains(kvp.Value.Resource))
                    array.AddResource(new UsedResourceData(FGResourceUsage.Read, kvp.Value.Resource));
            }

            t_globalResourceHash.Clear();
        }

        [ThreadStatic]
        private static HashSet<FrameGraphResource> t_globalResourceHash = new HashSet<FrameGraphResource>();
    }

    public readonly record struct RenderPassGroup(IndexRange Range, FrameGraphTexture FinalTexture, RenderPassRunContext RunContext, RenderContextContainer Context);
}
