using Arch.LowLevel;
using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Pooling;
using Primary.Profiling;
using Primary.Rendering.Data;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.Batching
{
    /*
     
    Shader -> Model (Mesh source) -> Material -> Mesh
     
    */

    public class RenderBatcher : IDisposable
    {
        private ConcurrentDictionary<MaterialAsset, int> _materialIndices;
        private Lock _materialLock;
        private List<MaterialAsset> _materialList;

        private ConcurrentDictionary<IRenderMeshSource, int> _meshSourceIndices;
        private Lock _meshSourceLock;
        private List<IRenderMeshSource> _meshSourceList;

        private DisposableObjectPool<ShaderRenderBatch> _renderBatchPool;
        private Dictionary<ShaderAsset, ShaderRenderBatch> _activeBatches;
        private PooledList<ShaderRenderBatch> _usedBatches;

        private Dictionary<ShaderAsset, ShaderKeyRange> _shaderIndices;
        //private UnsafeArray<RenderKey> _keys;
        private RenderKey[] _keys;

        private bool _disposedValue;

        internal RenderBatcher()
        {
            _materialIndices = new ConcurrentDictionary<MaterialAsset, int>();
            _materialLock = new Lock();
            _materialList = new List<MaterialAsset>();

            _meshSourceIndices = new ConcurrentDictionary<IRenderMeshSource, int>();
            _meshSourceLock = new Lock();
            _meshSourceList = new List<IRenderMeshSource>();

            _renderBatchPool = new DisposableObjectPool<ShaderRenderBatch>(new ShaderRenderBatch.PoolingPolicy());
            _activeBatches = new Dictionary<ShaderAsset, ShaderRenderBatch>();
            _usedBatches = new PooledList<ShaderRenderBatch>();

            _shaderIndices = new Dictionary<ShaderAsset, ShaderKeyRange>();
            //_keys = new UnsafeArray<RenderKey>(8);
            _keys = System.Array.Empty<RenderKey>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                _disposedValue = true;
            }
        }

        ~RenderBatcher()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearBatchData()
        {
            _materialIndices.Clear();
            _materialList.Clear();

            _meshSourceIndices.Clear();
            _meshSourceList.Clear();

            _usedBatches.Clear();
            _usedBatches.TrimExcess();

            _shaderIndices.Clear();
        }

        internal void BatchCollected(RenderTreeCollector collector)
        {
            using (new ProfilingScope("Batch"))
            {
                int totalKeys = 0;

                using (new ProfilingScope("Group"))
                {

                    foreach (RenderTreeSubCollector subCollector in collector.SubCollectors)
                    {
                        foreach (ShaderAsset shader in subCollector.UniqueShaders.Keys)
                        {
                            if (!_activeBatches.ContainsKey(shader))
                            {
                                ShaderRenderBatch renderBatch = _renderBatchPool.Get();
                                renderBatch.ResetForNextFrame(shader);

                                _activeBatches.Add(shader, renderBatch);
                                _usedBatches.Add(renderBatch);
                            }
                        }

                        totalKeys += subCollector.Keys.Length;
                    }

                    if (_keys.Length < totalKeys)
                        //_keys = UnsafeArray.Resize(ref _keys, totalKeys);
                        _keys = new RenderKey[totalKeys];

                    Span<RenderKey> keys = _keys.AsSpan().Slice(0, totalKeys);
                    foreach (RenderTreeSubCollector subCollector in collector.SubCollectors)
                    {
                        subCollector.Keys.CopyTo(keys);
                        keys = keys.Slice(subCollector.Keys.Length);
                    }
                }

                using (new ProfilingScope("Sort"))
                {
                    Span<RenderKey> keys = _keys.AsSpan().Slice(0, totalKeys);
                    keys.Sort();
                }

                using (new ProfilingScope("FindShaders"))
                {
                    ReadOnlySpan<RenderKey> keys = _keys.AsSpan().Slice(0, totalKeys);

                    int previousLastIndex = 0;
                    uint lastShaderIdx = uint.MaxValue;

                    for (int i = 0; i < keys.Length; i++)
                    {
                        ref readonly RenderKey key = ref keys[i];
                        if (key.ShaderId != lastShaderIdx)
                        {
                            RenderTreeSubCollector subCollector = collector.SubCollectors[key.CollectorIndex];
                            if (lastShaderIdx != uint.MaxValue)
                                _shaderIndices.Add(subCollector.Entities[previousLastIndex].Material.Shader!, new ShaderKeyRange(previousLastIndex, i));

                            previousLastIndex = i;
                            lastShaderIdx = key.ShaderId;
                        }
                    }

                    if (lastShaderIdx != uint.MaxValue && previousLastIndex != keys.Length)
                    {
                        ref readonly RenderKey key = ref keys[previousLastIndex];
                        RenderTreeSubCollector subCollector = collector.SubCollectors[key.CollectorIndex];
                        _shaderIndices.Add(subCollector.Entities[key.ListIndex].Material.Shader!, new ShaderKeyRange(previousLastIndex, keys.Length));
                    }
                }

                using (new ProfilingScope("Execute"))
                {
                    foreach (ShaderRenderBatch renderBatch in _activeBatches.Values)
                    {
                        renderBatch.Execute(this, collector.SubCollectors);
                    }
                }
            }
        }

        internal void CleanupPostFrame()
        {
            Debug.Assert(_usedBatches.Count == _activeBatches.Count);

            foreach (ShaderRenderBatch renderBatch in _usedBatches)
            {
                renderBatch.ClearData();
                _renderBatchPool.Return(renderBatch);
            }

            _activeBatches.Clear();

            ClearBatchData();
        }

        #region Interface
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal uint GetMaterialIndex(MaterialAsset material) //must keep under 32 bytes of IL
        {
            if (_materialIndices.TryGetValue(material, out int index))
                return (uint)index;
            else
                return GetMaterialIndex_SlowPath(material);
        }

        private uint GetMaterialIndex_SlowPath(MaterialAsset material)
        {
            return (uint)_materialIndices.GetOrAdd(material, (_) =>
            {
                using (_materialLock.EnterScope())
                {
                    int idx = _materialList.Count;

                    _materialList.Add(material);

                    return idx;
                }
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal uint GetMeshSourceIndex(IRenderMeshSource source) //refer to "GetMaterialIndex"
        {
            if (_meshSourceIndices.TryGetValue(source, out int index))
                return (uint)index;
            else
                return GetMeshSourceIndex_SlowPath(source);
        }

        private uint GetMeshSourceIndex_SlowPath(IRenderMeshSource source)
        {
            return (uint)_meshSourceIndices.GetOrAdd(source, (_) =>
            {
                using (_meshSourceLock.EnterScope())
                {
                    int idx = _meshSourceList.Count;

                    _meshSourceList.Add(source);
                    //_meshSourceIndices.TryAdd(source, idx);

                    return idx;
                }
            });
        }

        internal bool GetBaseIndexForShader(ShaderAsset shader, out ShaderKeyRange baseIndex)
        {
            return _shaderIndices.TryGetValue(shader, out baseIndex);
        }

        internal Span<RenderKey> GetKeyRange(ShaderKeyRange range)
        {
            return _keys.AsSpan().Slice(range.IdxStart, range.IdxEnd - range.IdxStart);
        }
        #endregion

        internal ReadOnlySpan<ShaderRenderBatch> UsedBatches => _usedBatches.Span;
    }

    internal readonly record struct UnbatchedRenderData(RawRenderMesh Mesh, MaterialAsset Material, Matrix4x4 Model);
    internal readonly record struct ShaderKeyRange(int IdxStart, int IdxEnd);
}
