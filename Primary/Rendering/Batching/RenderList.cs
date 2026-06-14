using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Rendering.Assets;
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

        private Dictionary<ShaderAsset, ShaderRenderBatcher> _shaderBatchers;
        private Dictionary<ShaderAsset, ShaderKeyRange> _shaderKeyRanges;

        private Dictionary<ShaderAsset, ushort> _shaderIds;
        private Dictionary<IRenderMeshSource, ushort> _modelIds;
        private Dictionary<MaterialAsset, uint> _materialIds;

        private Lock _shaderIdsLock;
        private Lock _modelIdsLock;
        private Lock _materialIdsLock;

        private List<ShaderRenderBatcher> _usedBatchers;

        private RenderKey[]? _rentedKeys;
        private int _rentedKeyCount;

        internal RenderList(BatchingManager manager)
        {
            _manager = manager;

            _defaultMaterial = AssetManager.LoadAsset<MaterialAsset>("Engine/Materials/R2DefaultMat.mat2", true);

            _shaderBatchers = new Dictionary<ShaderAsset, ShaderRenderBatcher>();
            _shaderKeyRanges = new Dictionary<ShaderAsset, ShaderKeyRange>();

            _shaderIds = new Dictionary<ShaderAsset, ushort>();
            _modelIds = new Dictionary<IRenderMeshSource, ushort>();
            _materialIds = new Dictionary<MaterialAsset, uint>();

            _shaderIdsLock = new Lock();
            _modelIdsLock = new Lock();
            _materialIdsLock = new Lock();

            _usedBatchers = new List<ShaderRenderBatcher>();

            _rentedKeys = null;
            _rentedKeyCount = 0;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    ClearFrameData();

                    foreach (ShaderRenderBatcher batcher in _shaderBatchers.Values)
                    {
                        batcher.Dispose();
                    }

                    _shaderBatchers.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearFrameData()
        {
            _shaderKeyRanges.Clear();

            _shaderIds.Clear();
            _modelIds.Clear();
            _materialIds.Clear();

            _usedBatchers.Clear();

            if (_rentedKeys != null)
                ReturnKeys();
        }

        internal void SetupShaderBatchers()
        {
            _usedBatchers.Clear();

            foreach (var kvp in _shaderIds)
            {
                if (!_shaderBatchers.TryGetValue(kvp.Key, out ShaderRenderBatcher? batcher))
                {
                    batcher = new ShaderRenderBatcher();
                    _shaderBatchers.Add(kvp.Key, batcher);
                }

                batcher.ClearFrameData(kvp.Key);
                _usedBatchers.Add(batcher);
            }
        }

        internal void ExecuteActiveBatchers(ReadOnlySpan<OctreeRenderBatcher> batchers)
        {
            foreach (var kvp in _shaderIds)
            {
                if (_shaderBatchers.TryGetValue(kvp.Key, out ShaderRenderBatcher? batcher))
                {
                    batcher.Execute(this, batchers);
                }

                Debug.Assert(batcher != null);
            }
        }

        internal void AddRange(ShaderAsset asset, ShaderKeyRange range)
        {
            _shaderKeyRanges.Add(asset, range);
        }

        internal Span<RenderKey> RentKeys(int count)
        {
            if (_rentedKeys != null)
                ArrayPool<RenderKey>.Shared.Return(_rentedKeys);

            _rentedKeys = ArrayPool<RenderKey>.Shared.Rent(count);
            _rentedKeyCount = count;

            return _rentedKeys.AsSpan(0, count);
        }

        internal void ReturnKeys()
        {
            if (_rentedKeys != null)
                ArrayPool<RenderKey>.Shared.Return(_rentedKeys);
            _rentedKeys = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ushort GetShaderId(ShaderAsset asset) => GetIdFromDictionaryWithLock(_shaderIds, _shaderIdsLock, asset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ushort GetModelId(IRenderMeshSource asset) => GetIdFromDictionaryWithLock(_modelIds, _modelIdsLock, asset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal uint GetMaterialId(MaterialAsset asset) => GetIdFromDictionaryWithLock(_materialIds, _materialIdsLock, asset);

        private TValue GetIdFromDictionaryWithLock<TKey, TValue>(Dictionary<TKey, TValue> dict, Lock @lock, TKey key) where TKey : class where TValue : struct, IUnsignedNumber<TValue>
        {
            // too large to inline anyway
            using var _ = @lock.EnterScope();

            ref TValue id = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out bool exists);
            if (exists)
                return id;

            if (typeof(TValue) == typeof(ushort))
                id = Unsafe.BitCast<ushort, TValue>((ushort)(dict.Count - 1));
            else if (typeof(TValue) == typeof(uint))
                id = Unsafe.BitCast<uint, TValue>((uint)(dict.Count - 1));
            else
                throw new NotSupportedException();

            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool GetKeyRangeForShader(ShaderAsset asset, out ShaderKeyRange keyRange) => _shaderKeyRanges.TryGetValue(asset, out keyRange);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span<RenderKey> GetSlicedKeyRange(ShaderKeyRange keyRange)
        {
            Debug.Assert(_rentedKeys != null);
            return _rentedKeys.AsSpan(keyRange.FlagIdxStart, keyRange.FlagIdxEnd - keyRange.FlagIdxStart);
        }

        public MaterialAsset? DefaultMaterial { get => _defaultMaterial; set => _defaultMaterial = value ?? AssetManager.LoadAsset<MaterialAsset>("Engine/Materials/R2DefaultMat.mat2", true); }

        public int TotalFlagCount => _rentedKeyCount;
        public ReadOnlySpan<ShaderRenderBatcher> ShaderBatchers => _usedBatchers.AsSpan();

        public IReadOnlyDictionary<ShaderAsset, ushort> ShaderIds => _shaderIds;
        public IReadOnlyDictionary<IRenderMeshSource, ushort> ModelIds => _modelIds;
        public IReadOnlyDictionary<MaterialAsset, uint> MaterialIds => _materialIds;
    }

    /*
        Primary (59 bits):
            ShaderId (16 bits / 65535)
            ModelId (16 bits / 65535)
            MaterialId (17 bits / 131070)
            MeshId (10 bits / 1023)
        Auxiliary: (1 bits):
            Transparent (1 bit / 1)
    */
    public struct RenderKey(ushort ShaderId, ushort ModelId, uint MaterialId, ushort MeshId, int ListIndex, byte BatcherIndex) : IComparable<RenderKey>, IComparer<RenderKey>, IEquatable<RenderKey>
    {
        public ulong Key = Create(ShaderId, ModelId, MaterialId, MeshId);
        public uint ListIndex = (((uint)BatcherIndex) << 31) | ((uint)ListIndex);

        public readonly ushort ShaderId => (ushort)((Key >> 48) & 0xffffu);
        public readonly ushort ModelId => (ushort)((Key >> 32) & 0xffffu);
        public readonly uint MaterialId => (uint)((Key >> 15) & 0x1ffffu);
        public readonly ushort MeshId => (ushort)((Key >> 5) & 0x400u);

        public readonly int Index => (int)((ListIndex) & 0x1ffffffu);
        public readonly int Batcher => (byte)((ListIndex >> 31) & 0x1u);

        public readonly int Compare(RenderKey x, RenderKey y) => x.Key.CompareTo(y.Key);
        public readonly int CompareTo(RenderKey other) => Key.CompareTo(other.Key);
        public readonly bool Equals(RenderKey other) => Key.Equals(other.Key);

        public static ulong Create(ushort shaderId, ushort modelId, uint materialId, ushort meshId)
        {
            return (((ulong)shaderId) << 48) | (((ulong)modelId) << 32) | (((ulong)materialId) << 15) | (((ulong)meshId) << 5);
        }
    }

    public readonly record struct UnbatchedRenderFlag(MaterialAsset Material, RawRenderMesh Mesh, Matrix4x4 Model);
    public readonly record struct ShaderKeyRange(int FlagIdxStart, int FlagIdxEnd);

    public ref struct ShaderRenderSection(ShaderAsset Shader, ReadOnlySpan<RenderSegment> Segments, ReadOnlySpan<RenderFlag> Flags)
    {
        public ShaderAsset Shader { get; init; } = Shader;
        public ReadOnlySpan<RenderSegment> Segments { get; init; } = Segments;
        public ReadOnlySpan<RenderFlag> Flags { get; init; } = Flags;
    }

    public readonly record struct RenderFlag(Matrix4x4 Matrix, uint DataId);
    public readonly record struct RenderSegment(MaterialAsset Material, RawRenderMesh Mesh, int FlagIndexStart, int FlagIndexEnd);
}
