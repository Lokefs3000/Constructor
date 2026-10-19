using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Pooling;

namespace Primary.Rendering.Passes
{
    public sealed class RenderPassIndex
    {
        private IRenderPass? _renderPass;
        private int _index;
        private string? _name;

        private object? _executeEntrypoint;
        private IPassData? _passData;

        private RenderContextContainer? _contextContainer;

        private RenderPassType _passType;

        private readonly List<ResourceHandle> _reads;
        private readonly List<ResourceHandle> _writes;
        private readonly List<ResourceHandle> _readWrites;

        private readonly HashSet<ResourceHandle> _resources;

        private readonly List<RenderPassIndex> _dependencies;
        private readonly List<RenderPassIndex> _successors;

        private bool _isAlive;
        private int _incomingEdgeCount;

        internal RenderPassIndex()
        {
            _renderPass = null;
            _index = 0;
            _name = null;

            _executeEntrypoint = null;
            _passData = null;

            _contextContainer = null;

            _passType = RenderPassType.Raster;

            _reads = new List<ResourceHandle>();
            _writes = new List<ResourceHandle>();
            _readWrites = new List<ResourceHandle>();

            _resources = new HashSet<ResourceHandle>();

            _dependencies = new List<RenderPassIndex>();
            _successors = new List<RenderPassIndex>();

            _isAlive = false;
            _incomingEdgeCount = 0;
        }

        internal void ResetForNewPass(IRenderPass? renderPass, int index, RenderPassType passType, string? name, RenderContextContainer? contextContainer)
        {
            _renderPass = renderPass;
            _index = index;
            _name = name;

            _executeEntrypoint = null;
            _passData = null;

            _contextContainer = contextContainer;

            _passType = passType;

            _reads.Clear();
            _writes.Clear();
            _readWrites.Clear();

            _resources.Clear();

            _dependencies.Clear();
            _successors.Clear();

            _isAlive = false;
            _incomingEdgeCount = 0;
        }

        internal void AddRead(ResourceHandle handle)
        {
            _reads.Add(handle);
            _resources.Add(handle);
        }

        internal void AddWrite(ResourceHandle handle)
        {
            _writes.Add(handle);
            _resources.Add(handle);
        }

        internal void AddReadWrite(ResourceHandle handle)
        {
            _readWrites.Add(handle);
            _resources.Add(handle);
        }

        internal void AddDependency(RenderPassIndex renderPass)
        {
            _dependencies.Add(renderPass);
        }

        internal void AddSuccessor(RenderPassIndex renderPass)
        {
            _successors.Add(renderPass);
        }

        internal void IncrementIncomingEdges()
        {
            ++_incomingEdgeCount;
        }

        internal void MarkAlive() => _isAlive = true;

        internal IRenderPass? Pass => _renderPass;
        internal int Index => _index;
        internal string Name => _name ?? string.Empty;

        internal Action<RasterPassContext>? RasterEntrypoint { get => (Action<RasterPassContext>?)_executeEntrypoint; set => _executeEntrypoint = value; }
        internal Action<ComputePassContext>? ComputeEntrypoint { get => (Action<ComputePassContext>?)_executeEntrypoint; set => _executeEntrypoint = value; }
        internal IPassData? PassData { get => _passData; set => _passData = value; }

        internal RenderContextContainer? ContextContainer => _contextContainer;

        internal RenderPassType PassType => _passType;

        internal ROList<ResourceHandle> Reads => _reads;
        internal ROList<ResourceHandle> Writes => _writes;
        internal ROList<ResourceHandle> ReadWrites => _readWrites;
        internal ROHashSet<ResourceHandle> Resources => _resources;

        internal ROList<RenderPassIndex> Dependencies => _dependencies;
        internal ROList<RenderPassIndex> Successors => _successors;

        public bool IsAlive => _isAlive;
        internal int IncomingEdgeCount => _incomingEdgeCount;

        internal sealed class GenericPoolPolicy : IObjectPoolPolicy<RenderPassIndex>
        {
            public RenderPassIndex Create() => new RenderPassIndex();

            public bool Return(ref RenderPassIndex obj)
            {
                obj.ResetForNewPass(null, -1, default, null, null);
                return true;
            }

            public static readonly GenericPoolPolicy Default = new GenericPoolPolicy();
        }
    }

    [Flags]
    public enum RenderPassType : byte
    {
        None = 0,

        Raster = 1 << 0,
        Compute = 1 << 1
    }
}
