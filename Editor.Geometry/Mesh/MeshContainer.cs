using CommunityToolkit.HighPerformance;
using Primary.Assets;
using Primary.Common;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;

namespace Editor.Geometry.Mesh
{
    public sealed class MeshContainer
    {
        private List<MeshSlice> _slices;

        private List<MeshFaceUpdate> _faceUpdates;
        private List<MeshVertexUpdate> _vertexUpdates;

        private Dictionary<BrushMeshKey, int> _brushFaceSliceIndices;

        private BrushVertex[] _vertices;

        private int _vertexCount;
        private int _newVertexCount;
 
        internal MeshContainer()
        {
            _slices = new List<MeshSlice>();

            _faceUpdates = new List<MeshFaceUpdate>();
            _vertexUpdates = new List<MeshVertexUpdate>();

            _brushFaceSliceIndices = new Dictionary<BrushMeshKey, int>();

            _vertices = Array.Empty<BrushVertex>();

            _vertexCount = 0;
            _newVertexCount = 0;
        }

        internal void ClearMeshData()
        {
            _slices.Clear();

            _faceUpdates.Clear();
            _vertexUpdates.Clear();

            _brushFaceSliceIndices.Clear();

            _vertices = Array.Empty<BrushVertex>();

            _vertexCount = 0;
            _newVertexCount = 0;
        }

        internal void ClearPreviousData()
        {
            _faceUpdates.Clear();
            _vertexUpdates.Clear();

            _newVertexCount = 0;
        }

        internal void AddBrushFace(BrushMeshKey key, MaterialAsset material)
        {
            Span<MeshSlice> slices = _slices.AsSpan();
            for (int i = 0; i < slices.Length; i++)
            {
                ref MeshSlice slice = ref slices[i];
                if (slice.Material == material && slice.Group == key.Brush.Group)
                {
                    slice.Updates.Add(new MeshSliceFaceUpdate(key, -1));
                    return;
                }
            }

            int lastIndex = _slices.Count > 0 ? _slices[^1].VtxOffset + _slices[^1].VtxCount : 0;

            MeshSlice newSlice = new MeshSlice(key.Brush.Group, material, int.MaxValue, 0, [], [new MeshSliceFaceUpdate(key, -1)], MeshSliceUpdateFlags.None);
            _slices.Add(newSlice);
        }

        internal void RemoveBrushFace(BrushMeshKey key)
        {
            if (_brushFaceSliceIndices.TryGetValue(key, out int sliceIndex))
            {
                Debug.Assert(sliceIndex >= 0 && sliceIndex < _slices.Count);

                ref MeshSlice slice = ref _slices.AsSpan()[sliceIndex];
                int index = slice.FindFaceIndex(key);

                Debug.Assert(index != -1);
                if (index != -1)
                    slice.Updates.Add(new MeshSliceFaceUpdate(key, index));
            }
        }

        internal void MoveBrushFace(BrushMeshKey key, MaterialAsset material)
        {
            if (_brushFaceSliceIndices.TryGetValue(key, out int sliceIndex))
            {
                Debug.Assert(sliceIndex >= 0 && sliceIndex < _slices.Count);

                ref MeshSlice slice = ref _slices.AsSpan()[sliceIndex];
                int index = slice.FindFaceIndex(key);

                Debug.Assert(index != -1);
                if (index != -1)
                    slice.Updates.Add(new MeshSliceFaceUpdate(key, index | FlagKeepBrushInDictionary));
            }

            AddBrushFace(key, material);
        }

        internal void AddFaceUpdate(BrushMeshKey key, int sliceIndex)
        {
            ref MeshSlice slice = ref _slices.AsSpan()[sliceIndex];
            int index = slice.FindFaceIndex(key);

            if (index != -1)
                slice.Updates.Add(new MeshSliceFaceUpdate(key, index | FlagKeepBrush));
        }

        internal void UpdateCurrentData()
        {
            int currentRangeOffset = 0;
            _newVertexCount = 0;

            Span<MeshSlice> slices = _slices.AsSpan();
            for (int i = 0; i < slices.Length; ++i)
            {
                ref MeshSlice slice = ref slices[i];
                slice.UpdateFlags = MeshSliceUpdateFlags.None;

                if (slice.VtxOffset == int.MaxValue)
                {
                    Debug.Assert(slice.VtxCount == 0);
                    slice.VtxOffset = currentRangeOffset;
                }

                int vtxDiff = 0;
                if (slice.VtxOffset > currentRangeOffset)
                    vtxDiff = slice.VtxOffset - currentRangeOffset;

                int vertexUpdateStartIndex = _vertexUpdates.Count;

                if (slice.Updates.Count > 0)
                {
                    slice.Updates.Sort(static (x, y) => (x.Index & ~FlagMask).CompareTo(y.Index & ~FlagMask));

                    int indexOffset = 0;
                    int updateStartIndex = _faceUpdates.Count;

                    foreach (MeshSliceFaceUpdate update in slice.Updates)
                    {
                        if (update.Index == -1)
                        {
                            _faceUpdates.Add(new MeshFaceUpdate(update.Key, i, slice.VtxCount + vtxDiff));
                            _brushFaceSliceIndices[update.Key] = i;

                            slice.BrushFaces.Add(update.Key);
                            slice.VtxCount += 6;
                            slice.UpdateFlags |= MeshSliceUpdateFlags.VerticesChanged;

                            _newVertexCount += 6;
                        }
                        else if (Flags.HasFlag(update.Index, FlagKeepBrush))
                        {
                            _faceUpdates.Add(new MeshFaceUpdate(update.Key, i, ((update.Index & ~FlagMask) + vtxDiff) * 6));
                            _newVertexCount += 6;

                            slice.UpdateFlags |= MeshSliceUpdateFlags.VerticesChanged;
                        }
                        else
                        {
                            int index = update.Index & ~FlagMask;
                            if (update.Index == index)
                                _brushFaceSliceIndices.Remove(update.Key);

                            index += indexOffset;
                            if (index != slice.Updates.Count - 1)
                            {
                                MeshVertexUpdate vertexUpdate = new MeshVertexUpdate((index + 1) * 6, index * 6, (slice.Updates.Count - index - 1) * 6);
                                _vertexUpdates.Add(vertexUpdate);

                                Array.Copy(_vertices, vertexUpdate.SourceIndex, _vertices, vertexUpdate.DestinationIndex, vertexUpdate.Count);
                                slice.UpdateFlags |= MeshSliceUpdateFlags.VerticesChanged | MeshSliceUpdateFlags.MemoryOffsetChanged;
                            }

                            slice.BrushFaces.RemoveAt(index);
                            --indexOffset;

                            slice.VtxCount -= 6;
                            vtxDiff -= 1;
                        }
                    }

                    //{
                    //    Span<MeshFaceUpdate> updates = _faceUpdates.AsSpan()[updateStartIndex..];
                    //    for (int j = 0; j < updates.Length; ++j)
                    //    {
                    //        ref MeshFaceUpdate update = ref updates[j];
                    //        update = new MeshFaceUpdate(update.Key, update.SliceIndex, update.VertexOffset + vtxDiff);
                    //    }
                    //}

                    slice.Updates.Clear();
                }

                if (slice.VtxCount == 0)
                {
                    if (i != _slices.Count - 1)
                    {
                        for (int j = i + 1; j < _slices.Count; ++j)
                        {
                            ref MeshSlice prevSlice = ref slices[j];
                            int newIndex = j - 1;

                            for (int k = 0; k < prevSlice.BrushFaces.Count; k++)
                            {
                                _brushFaceSliceIndices[prevSlice.BrushFaces[k]] = newIndex;
                            }
                        }

                        Span<MeshFaceUpdate> updates = _faceUpdates.AsSpan();

                        int removalIndex = -1;
                        for (int j = _faceUpdates.Count - 1; j >= 0; --j)
                        {
                            if (_faceUpdates[j].SliceIndex == i)
                                removalIndex = j;
                        }

                        if (removalIndex != -1)
                            _faceUpdates.RemoveRange(removalIndex, _faceUpdates.Count - removalIndex);
                        if (vertexUpdateStartIndex < _vertexUpdates.Count)
                            _vertexUpdates.RemoveRange(++vertexUpdateStartIndex, _vertexUpdates.Count - vertexUpdateStartIndex);

                        _slices.RemoveAt(i--);
                        slices = _slices.AsSpan();
                    }
                }
                else if (slice.VtxOffset > currentRangeOffset)
                {
                    MeshVertexUpdate vertexUpdate = new MeshVertexUpdate(slice.VtxOffset, currentRangeOffset, slice.VtxCount);
                    _vertexUpdates.Add(vertexUpdate);

                    Array.Copy(_vertices, vertexUpdate.SourceIndex, _vertices, vertexUpdate.DestinationIndex, vertexUpdate.Count);
                    slice.UpdateFlags |= MeshSliceUpdateFlags.MemoryOffsetChanged;

                    slice.VtxOffset = currentRangeOffset;
                    currentRangeOffset += slice.VtxCount;
                }
                else
                    currentRangeOffset += slice.VtxCount;
            }

            _vertexCount = currentRangeOffset;

            if (_vertices.Length < _vertexCount)
            {
                int rounded = (int)BitOperations.RoundUpToPowerOf2((uint)_vertexCount);
                Array.Resize(ref _vertices, rounded);
            }
        }

        internal bool TryGetBrushSliceIndex(BrushMeshKey key, out int index) => _brushFaceSliceIndices.TryGetValue(key, out index);

        public Span<MeshSlice> Slices => _slices.AsSpan();

        public Span<MeshVertexUpdate> VertexUpdates => _vertexUpdates.AsSpan();
        public Span<MeshFaceUpdate> FaceUpdates => _faceUpdates.AsSpan();

        public Span<BrushVertex> Vertices => _vertices.AsSpan(0, _vertexCount);

        public int VertexCount => _vertexCount;
        public int NewVertexCount => _newVertexCount;

        public static AABB GetAABB(MeshContainer container, MeshSlice slice)
        {
            if (slice.VtxCount == 0)
                return AABB.Zero;

            Vector256<float> aabb = Vector256.Create(float.NegativeInfinity);

            Span<BrushVertex> vertices = container._vertices.AsSpan(slice.VtxOffset, slice.VtxCount);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector128<float> vertex = vertices.DangerousGetReferenceAt(i).Position.AsVector128Unsafe();
                aabb = Vector256.Max(aabb, Vector256.Create(-vertex, vertex));
            }

            return new AABB(-aabb.GetLower().AsVector3(), aabb.GetUpper().AsVector3());
        }

        private const int FlagKeepBrush = 1 << 29;
        private const int FlagKeepBrushInDictionary = 1 << 30;

        private const int FlagMask = FlagKeepBrush | FlagKeepBrushInDictionary;
    }

    public record struct MeshSlice(BrushGroup Group, MaterialAsset Material, int VtxOffset, int VtxCount, List<BrushMeshKey> BrushFaces, List<MeshSliceFaceUpdate> Updates, MeshSliceUpdateFlags UpdateFlags)
    {
        public int FindFaceIndex(BrushMeshKey key)
        {
            for (int i = 0; i < BrushFaces.Count; i++)
            {
                if (BrushFaces[i] == key)
                    return i;
            }

            return -1;
        }
    }

    [Flags]
    public enum MeshSliceUpdateFlags : byte
    {
        None = 0,

        VerticesChanged = 1 << 0,
        MemoryOffsetChanged = 1 << 1
    }

    public readonly record struct MeshSliceFaceUpdate(BrushMeshKey Key, int Index);

    public readonly record struct BrushMeshKey(Brush Brush, BrushFaceIndex FaceIndex)
    {
        public override int GetHashCode() => HashCode.Combine(Brush, FaceIndex);
    }
}
