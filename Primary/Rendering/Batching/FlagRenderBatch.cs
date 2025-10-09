using Arch.LowLevel;
using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Rendering.Data;
using SharpGen.Runtime;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Batching
{
    public unsafe sealed class FlagRenderBatch : IDisposable
    {
        private ShaderAsset? _shaderReference;

        private Stack<RenderMeshBatchData> _storedBatchData;
        //TODO: use ConcurrentDictionary instead for mystery speedups?
        private ConcurrentDictionary<nint, RenderMeshBatchData> _activeBatches;

        private List<RenderMeshBatchData> _usedBatchDatas;

        private bool _disposedValue;

        internal FlagRenderBatch()
        {
            _shaderReference = null;

            _storedBatchData = new Stack<RenderMeshBatchData>();
            _activeBatches = new ConcurrentDictionary<nint, RenderMeshBatchData>();

            _usedBatchDatas = new List<RenderMeshBatchData>();
        }

        internal void ResetForNextFrame(ShaderAsset shader)
        {
            _shaderReference = shader;

            foreach (var kvp in _activeBatches)
            {
                kvp.Value.Reset();
                _storedBatchData.Push(kvp.Value);
            }

            _usedBatchDatas.Clear();
            _activeBatches.Clear();
        }

        internal void ClearData()
        {
            _shaderReference = null;

            foreach (var kvp in _activeBatches)
            {
                kvp.Value.Reset();
                _storedBatchData.Push(kvp.Value);
            }

            _usedBatchDatas.Clear();
            _activeBatches.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddFlag(RawRenderMesh mesh, uint matIdx, ref Matrix4x4 model)
        {
            if (!_activeBatches.TryGetValue(mesh.Handle, out RenderMeshBatchData? batchData))
                AppendNewBatch_SlowPath(mesh, out batchData);
            batchData.Add(ref model, ref matIdx);

            void AppendNewBatch_SlowPath(RawRenderMesh mesh, out RenderMeshBatchData batchData)
            {
                if (!_storedBatchData.TryPop(out batchData!))
                    batchData = new RenderMeshBatchData();

                bool r = _activeBatches.TryAdd(mesh.Handle, batchData);
                Debug.Assert(r);

                _usedBatchDatas.Add(batchData); //TODO: if multithreading this function add semaphore!

                batchData.Mesh = mesh;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                ClearData();

                while (_storedBatchData.TryPop(out RenderMeshBatchData? batchData))
                    batchData.Dispose();

                _disposedValue = true;
            }
        }

        ~FlagRenderBatch()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal ShaderAsset? ShaderReference => _shaderReference;
        internal Span<RenderMeshBatchData> RenderMeshBatches => _usedBatchDatas.AsSpan();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal record struct BatchedRenderFlag
    {
        public Matrix4x4 Model;
        public uint MaterialIndex;
    }

    internal class RenderMeshBatchData : IDisposable
    {
        public RawRenderMesh? Mesh = null;
        public UnsafeList<BatchedRenderFlag> BatchableFlags;

        internal RenderMeshBatchData()
        {
            Mesh = null;
            BatchableFlags = new UnsafeList<BatchedRenderFlag>(32);
        }

        public void Dispose()
        {
            Mesh = null;
            BatchableFlags.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Reset()
        {
            Mesh = null;
            BatchableFlags.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add(ref Matrix4x4 model, ref uint matIndex)
        {
            BatchableFlags.Add(new BatchedRenderFlag { Model = model, MaterialIndex = matIndex });
        }
    }
}
