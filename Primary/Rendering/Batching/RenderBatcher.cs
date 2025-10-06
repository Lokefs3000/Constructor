using CommunityToolkit.HighPerformance;
using Primary.Assets;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Batching
{
    public class RenderBatcher : IDisposable
    {
        private SemaphoreSlim _semaphore;
        private Stack<FlagRenderBatch> _storedBatches;

        private ConcurrentDictionary<int, FlagRenderBatch> _activeBatches;
        private ConcurrentDictionary<nint, uint> _activeMaterials;

        private ConcurrentDictionary<uint, MaterialAsset> _materialDict;

        private List<FlagRenderBatch> _usedBatches;

        private bool _disposedValue;

        internal RenderBatcher()
        {
            _semaphore = new SemaphoreSlim(1);
            _storedBatches = new Stack<FlagRenderBatch>();

            _activeBatches = new ConcurrentDictionary<int, FlagRenderBatch>();
            _activeMaterials = new ConcurrentDictionary<nint, uint>();

            _materialDict = new ConcurrentDictionary<uint, MaterialAsset>();

            _usedBatches = new List<FlagRenderBatch>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal FlagRenderBatch GetFlagBatch(ShaderAsset shader)
        {
            if (!_activeBatches.TryGetValue(shader.HashCode, out FlagRenderBatch? batch))
                CreateBatch_SlowPath(shader, out batch);
            return batch;

            void CreateBatch_SlowPath(ShaderAsset shader, out FlagRenderBatch batch)
            {
                _semaphore.Wait();
                if (!_storedBatches.TryPop(out batch!))
                    batch = new FlagRenderBatch();

                batch.ResetForNextFrame(shader);

                bool r = _activeBatches.TryAdd(shader.HashCode, batch!);
                Debug.Assert(r);

                _usedBatches.Add(batch);

                _semaphore.Release();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal uint GetMaterialIndex(MaterialAsset material)
        {
            return _activeMaterials.GetOrAdd(material.Handle, GetMaterialIndex_Callback);
        }

        private uint GetMaterialIndex_Callback(nint x)
        {
            uint id = (uint)_activeMaterials.Count;

            bool r = _materialDict.TryAdd(id, Unsafe.As<MaterialAsset>(GCHandle.FromIntPtr(x).Target)!);
            Debug.Assert(r);

            return id;
        }

        internal void ClearBatchData()
        {
            foreach (var kvp in _activeBatches)
            {
                kvp.Value.ClearData();
                _storedBatches.Push(kvp.Value);
            }

            _activeBatches.Clear();
            _activeMaterials.Clear();
            _usedBatches.Clear();

            _materialDict.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal MaterialAsset? GetMaterialFromIndex(uint index)
        {
            _materialDict.TryGetValue(index, out MaterialAsset? material);
            return material;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _activeMaterials.Clear();
                    _activeBatches.Clear();
                    _usedBatches.Clear();
                }

                ClearBatchData();

                while (_storedBatches.TryPop(out FlagRenderBatch? batch))
                    batch.Dispose();

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

        internal Span<FlagRenderBatch> UsedBatches => _usedBatches.AsSpan();
    }
}
