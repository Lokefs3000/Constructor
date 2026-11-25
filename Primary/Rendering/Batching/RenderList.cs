using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Memory;
using Primary.Profiling;
using Primary.Rendering.Assets;
using Primary.Threading;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.Rendering.Batching
{
    public sealed class RenderList : IDisposable
    {
        private readonly BatchingManager _manager;
        private bool _disposedValue;

        private MaterialAsset? _defaultMaterial;

        private readonly ConcurrentBag<GridChunkBatcher> _finishedBatchers;
        
        private readonly Dictionary<RenderMesh, int> _batchMetrics;
        private int _totalMatrixCount;

        private readonly HashSet<int> _uniqueMaterials;
        private readonly List<MaterialAsset> _materials;
        private int _materialByteSize;

        private readonly Dictionary<RenderMesh, int> _renderMeshOffsets;
        private readonly List<RenderListRange> _renderListRanges;

        private Matrix4x4[] _matricies;

        internal RenderList(BatchingManager manager)
        {
            _manager = manager;

            _defaultMaterial = null;

            _finishedBatchers = new ConcurrentBag<GridChunkBatcher>();
            _batchMetrics = new Dictionary<RenderMesh, int>();

            _uniqueMaterials = new HashSet<int>();
            _materials = new List<MaterialAsset>();
            _materialByteSize = 0;

            _renderMeshOffsets = new Dictionary<RenderMesh, int>();
            _renderListRanges = new List<RenderListRange>();

            _matricies = [];
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Clear();
                    _manager.RemoveRenderList(this);
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Clear()
        {
            while (_finishedBatchers.TryTake(out GridChunkBatcher? chunkBatcher))
            {
                _manager.ReturnGridChunkBatcher(chunkBatcher);
            }

            _batchMetrics.Clear();
            _totalMatrixCount = 0;

            _uniqueMaterials.Clear();
            _materials.Clear();
            _materialByteSize = 0;

            _renderMeshOffsets.Clear();
            _renderListRanges.Clear();
        }

        internal void FinishBatching(GridChunkBatcher chunkBatcher)
        {
            _finishedBatchers.Add(chunkBatcher);
        }

        private void CalculateMetrics(ReadOnlySpan<GridChunkBatcher> chunkBatchers)
        {
            foreach (GridChunkBatcher chunkBatcher in chunkBatchers)
            {
                foreach (var (renderMesh, matricies) in chunkBatcher.Batches)
                {
                    ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(_batchMetrics, renderMesh, out bool exists);
                    if (!exists)
                    {
                        count = 0;
                    }

                    count += matricies.Count;
                    _totalMatrixCount += matricies.Count;

                    int materialIndex = MaterialPass.GetMaterialIndex(renderMesh.PassId);
                    if (_uniqueMaterials.Add(materialIndex))
                    {
                        MaterialAsset asset = AssetManager.GetAssetFromIndex<MaterialAsset>(materialIndex)!;

                        _materials.Add(asset);
                        _materialByteSize += asset.GetPropertyBlockOrNull(MaterialPass.GetShaderType(renderMesh.PassId)).BlockSize;
                    }
                }
            }
        }

        private void CombineBatches(ReadOnlySpan<GridChunkBatcher> chunkBatchers)
        {
            MaterialPassCollection materialPasses = Engine.GlobalSingleton.RenderingManager.Services.GetService<MaterialPassCollection>();

            int currentOffset = 0;

            if (_matricies.Length < _totalMatrixCount)
            {
                _matricies = new Matrix4x4[MemoryHelper.AlignUp(_totalMatrixCount, 256)];
            }

            foreach (GridChunkBatcher chunkBatcher in chunkBatchers)
            {
                foreach (var (renderMesh, matricies) in chunkBatcher.Batches)
                {
                    ref int offset = ref CollectionsMarshal.GetValueRefOrAddDefault(_renderMeshOffsets, renderMesh, out bool exists);
                    if (!exists || offset == -1)
                    {
                        if (_batchMetrics.TryGetValue(renderMesh, out int totalMatrixCount) &&
                            materialPasses.TryGetPass(renderMesh.PassId, out MaterialPass pass))
                        {
                            ModelAsset model = AssetManager.GetAssetFromIndex<ModelAsset>(renderMesh.ModelIndex)!;
                            if (!model.IsReady || renderMesh.MeshIndex >= model.Meshes.Length)
                                continue;

                            _renderListRanges.Add(new RenderListRange(pass, model, model.Meshes[renderMesh.MeshIndex].Asset.Value, currentOffset, totalMatrixCount));

                            offset = currentOffset;
                            currentOffset += totalMatrixCount;
                        }
                        else
                        {
                            offset = -1;
                            continue;
                        }
                    }

                    matricies.Span.CopyTo(_matricies.AsSpan(offset, matricies.Count));
                    offset += matricies.Count;
                }
            }
        }

#if DEBUG
        internal int FinishedBatchers => _finishedBatchers.Count;
#endif

        public int MaterialByteSize => _materialByteSize;
        public ROList<MaterialAsset> Materials => _materials;
        public int MaterialCount => _uniqueMaterials.Count;

        public ROList<RenderListRange> Ranges => _renderListRanges;
        public ReadOnlySpan<Matrix4x4> Matricies => _matricies.AsSpan();

        public bool IsEmpty => _renderListRanges.Count == 0 || _totalMatrixCount == 0;

        internal readonly record struct CombineGridBatches(RenderList RenderList) : IJob
        {
            public void Execute()
            {
                using (new ProfilingScope("CombineGridBatches"))
                {
                    using RentedList<GridChunkBatcher> chunkBatchers = new RentedList<GridChunkBatcher>();
                    while (RenderList._finishedBatchers.TryTake(out GridChunkBatcher? chunkBatcher))
                    {
                        chunkBatchers.Add(chunkBatcher);
                    }

                    using (new ProfilingScope("Metrics"))
                    {
                        RenderList.CalculateMetrics(chunkBatchers.AsSpan());
                    }

                    using (new ProfilingScope("Combine"))
                    {
                        RenderList.CombineBatches(chunkBatchers.AsSpan());
                    }
                }
            }
        }
    }

    public readonly record struct RenderListRange(MaterialPass Pass, ModelAsset Model, MeshAsset Mesh, int MatrixOffset, int InstanceCount);
}
