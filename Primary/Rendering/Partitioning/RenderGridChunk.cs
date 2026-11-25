using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using Arch.Core;
using Arch.LowLevel.Jagged;
using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Collections.Extensions;
using Primary.Collections.ReadOnly;
using Primary.Components;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Rendering.Batching;
using Primary.Rendering.Utility;
using Primary.Scenes;
using Primary.Timing;
using Primary.Utility;

namespace Primary.Rendering.Partitioning
{
    public sealed class RenderGridChunk
    {
        private readonly RenderingManager _manager;

        private readonly RenderExtents _sourceExtents;
        private readonly Int3 _gridPoint;

        private RenderExtents _currentExtents;

        private int _entityCount;

        private bool _wantsExtentsRefit;

        private readonly IdCounter _ids;
        private readonly JaggedArray<RenderableSlot> _slots;
        private readonly List<RenderableChunk> _chunks;

        private readonly Dictionary<RenderMesh, PooledSwapAndPopList<int>> _redirectionLists;

        private RentedList<RenderableUpdateData> _updates;

        internal RenderGridChunk(RenderingManager manager, ArrayPool<Renderable> entityArrayPool, Int3 gridPoint)
        {
            _manager = manager;

            _sourceExtents = new RenderExtents(gridPoint.AsVector3() * GridPartitionSize, new Vector3(GridPartitionSize / 2), new Vector3(GridPartitionSize / 2).LengthSquared());
            _gridPoint = gridPoint;

            _currentExtents = _sourceExtents;

            _entityCount = 0;

            _wantsExtentsRefit = false;

            _ids = new IdCounter();
            _slots = new JaggedArray<RenderableSlot>(1024 / Unsafe.SizeOf<RenderableSlot>(), new RenderableSlot(-1, -1));
            _chunks = new List<RenderableChunk>();

            _redirectionLists = new Dictionary<RenderMesh, PooledSwapAndPopList<int>>();

            _updates = new RentedList<RenderableUpdateData>();
        }

        internal void FlushUpdates()
        {
            using (new ProfilingScope("FlushUpdates"))
            {
                MaterialPassCollection materialPasses = _manager.Services.GetService<MaterialPassCollection>();

                for (int i = 0; i < _updates.Count; ++i)
                {
                    ref RenderableUpdateData updateData = ref _updates[i];
                    Debug.Assert(updateData.Chunk == this);

                    switch (updateData.UpdateType)
                    {
                        case RenderableUpdateData.Type.Create:
                            {
                                SceneEntity entity = updateData.Entity;
                                ref EntityData entityData = ref World.Worlds.DangerousGetReferenceAt(entity.WrappedEntity.WorldId).GetEntityData(entity.WrappedEntity);

                                ref MeshRenderer meshRendererComp = ref entityData.Get<MeshRenderer>();
                                if (meshRendererComp.Mesh != null && meshRendererComp.Material != null && materialPasses.TryGetMaterial(meshRendererComp.Material, out ReadOnlySpan<MaterialPass?> passes))
                                {
                                    ref RenderBounds boundsComp = ref entityData.Get<RenderBounds>();
                                    ref WorldTransform transformComp = ref entityData.Get<WorldTransform>();
                                    ref RenderOctantInfo octantInfoComp = ref entityData.Get<RenderOctantInfo>();

                                    ref Renderable renderable = ref CreateRenderable();
                                    renderable.Extents = new RenderExtents(boundsComp.ComputedBounds);
                                    renderable.ModelIndex = meshRendererComp.Mesh.ParentAsset!.UniqueIndex;
                                    renderable.MeshIndex = (ushort)meshRendererComp.Mesh.MeshIndex;
                                    renderable.MaterialPasses = new Common.RentedArray<RenderablePassIndex>(passes.Length);
                                    renderable.RenderLayers = meshRendererComp.Layers;
                                    renderable.Transform = transformComp.Transformation;

                                    for (int j = 0; j < passes.Length; ++j)
                                    {
                                        ref readonly MaterialPass? passNullable = ref passes[j];
                                        if (passNullable.HasValue)
                                        {
                                            MaterialPass pass = passNullable.GetValueOrDefault();
                                            RenderMesh renderMesh = new RenderMesh(pass.PassId, renderable.ModelIndex, renderable.MeshIndex);

                                            ref PooledSwapAndPopList<int> redirectionList = ref CollectionsMarshal.GetValueRefOrAddDefault(_redirectionLists, renderMesh, out bool exists);
                                            if (!exists)
                                            {
                                                redirectionList = new PooledSwapAndPopList<int>(16);
                                            }

                                            int index = redirectionList.Add(renderable.Id);
                                            renderable.MaterialPasses[j] = new RenderablePassIndex(pass.PassId, index);
                                        }
                                        else
                                        {
                                            renderable.MaterialPasses[j] = new RenderablePassIndex(ulong.MaxValue, -1);
                                        }
                                    }

                                    octantInfoComp.RenderableId = renderable.Id;
                                    octantInfoComp.Chunk = this;
                                    octantInfoComp.LastUpdateFrame = Time.FrameIndex;

                                    if (DoesExtendBeyondBounds(_currentExtents, renderable.Extents.Center, renderable.Extents.SqrRadius))
                                    {
                                        _wantsExtentsRefit = true;
                                    }
                                }

                                break;
                            }
                        case RenderableUpdateData.Type.Destroy:
                            {
                                ref Renderable renderable = ref GetRenderable(updateData.RenderableId);
                                for (int j = 0; j < renderable.MaterialPasses.Count; ++j)
                                {
                                    ref RenderablePassIndex materialPass = ref renderable.MaterialPasses[j];
                                    if (materialPass.PassId == ulong.MaxValue || materialPass.ArrayIndex == -1)
                                        continue;

                                    RenderMesh renderMesh = new RenderMesh(materialPass.PassId, renderable.ModelIndex, renderable.MeshIndex);
                                    ref PooledSwapAndPopList<int> redirectionList = ref CollectionsMarshal.GetValueRefOrNullRef(_redirectionLists, renderMesh);
                                    if (!Unsafe.IsNullRef(in redirectionList))
                                    {
                                        redirectionList.RemoveAt(materialPass.ArrayIndex, out int movedIndex);
                                        if (movedIndex != -1)
                                        {
                                            ref Renderable movedRenderable = ref GetRenderable(redirectionList[movedIndex]);
                                            movedRenderable.MaterialPasses[j] = new RenderablePassIndex(movedRenderable.MaterialPasses[j].PassId, movedIndex);
                                        }
                                        else if (redirectionList.IsEmpty)
                                        {
                                            redirectionList.Dispose();
                                            _redirectionLists.Remove(renderMesh);
                                        }
                                    }
                                }

                                if (DoesExtendBeyondBounds(_sourceExtents, renderable.Extents.Center, renderable.Extents.SqrRadius))
                                {
                                    _wantsExtentsRefit = true;
                                }

                                DestroyRenderable(updateData.RenderableId);

                                break;
                            }
                        case RenderableUpdateData.Type.Update:
                            {
                                SceneEntity entity = updateData.Entity;
                                ref EntityData entityData = ref World.Worlds.DangerousGetReferenceAt(entity.WrappedEntity.WorldId).GetEntityData(entity.WrappedEntity);

                                ref MeshRenderer meshRendererComp = ref entityData.Get<MeshRenderer>();
                                if (meshRendererComp.Mesh != null && meshRendererComp.Material != null && materialPasses.TryGetMaterial(meshRendererComp.Material, out ReadOnlySpan<MaterialPass?> passes))
                                {
                                    ref Renderable renderable = ref GetRenderable(updateData.RenderableId);
                                    
                                    int prevModelIndex = renderable.ModelIndex;
                                    ushort prevMeshIndex = renderable.MeshIndex;
                                    renderable.ModelIndex = meshRendererComp.Mesh.ParentAsset!.UniqueIndex;
                                    renderable.MeshIndex = (ushort)meshRendererComp.Mesh.MeshIndex;

                                    for (int j = 0; j < renderable.MaterialPasses.Count; ++j)
                                    {
                                        {
                                            ref RenderablePassIndex materialPass = ref renderable.MaterialPasses[j];
                                            if (materialPass.PassId == ulong.MaxValue || materialPass.ArrayIndex == -1)
                                                goto AddStep;

                                            RenderMesh renderMesh = new RenderMesh(materialPass.PassId, prevModelIndex, prevMeshIndex);
                                            ref PooledSwapAndPopList<int> redirectionList = ref CollectionsMarshal.GetValueRefOrNullRef(_redirectionLists, renderMesh);
                                            if (!Unsafe.IsNullRef(in redirectionList))
                                            {
                                                redirectionList.RemoveAt(materialPass.ArrayIndex, out int movedIndex);
                                                if (movedIndex != -1)
                                                {
                                                    ref Renderable movedRenderable = ref GetRenderable(redirectionList[movedIndex]);
                                                    movedRenderable.MaterialPasses[j] = new RenderablePassIndex(movedRenderable.MaterialPasses[j].PassId, movedIndex);
                                                }
                                                else if (redirectionList.IsEmpty)
                                                {
                                                    redirectionList.Dispose();
                                                    _redirectionLists.Remove(renderMesh);
                                                }
                                            }
                                        }

                                    AddStep:
                                        {
                                            ref readonly MaterialPass? passNullable = ref passes[j];
                                            if (passNullable.HasValue)
                                            {
                                                MaterialPass pass = passNullable.GetValueOrDefault();
                                                RenderMesh renderMesh = new RenderMesh(pass.PassId, renderable.ModelIndex, renderable.MeshIndex);

                                                ref PooledSwapAndPopList<int> redirectionList = ref CollectionsMarshal.GetValueRefOrAddDefault(_redirectionLists, renderMesh, out bool exists);
                                                if (!exists)
                                                {
                                                    redirectionList = new PooledSwapAndPopList<int>(16);
                                                }

                                                int index = redirectionList.Add(renderable.Id);
                                                renderable.MaterialPasses[j] = new RenderablePassIndex(pass.PassId, index);
                                            }
                                            else
                                            {
                                                renderable.MaterialPasses[j] = new RenderablePassIndex(ulong.MaxValue, -1);
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                    }
                }

                _updates.Dispose();
            }
        }

        internal void AddUpdate(in RenderableUpdateData updateData) => _updates.Add(updateData);
 
        internal void FitExtents(RenderExtents extents)
        {
            if (DoesExtendBeyondBounds(_sourceExtents, extents.Center, extents.SqrRadius))
            {
                _wantsExtentsRefit = true;
            }
        }

        internal void RefitExtents()
        {
            using (new ProfilingScope("RefitExtents"))
            {
                Vector3 min = _sourceExtents.Center;
                Vector3 max = _sourceExtents.Center;

                foreach (ref RenderableChunk chunk in _chunks.AsSpan())
                {
                    foreach (ref Renderable entity in chunk.Renderables)
                    {
                        min = Vector3.Min(min, entity.Extents.Center - entity.Extents.HalfExtents);
                        max = Vector3.Max(max, entity.Extents.Center + entity.Extents.HalfExtents);
                    }
                }

                _currentExtents = new RenderExtents(new AABB(min, max));
                _wantsExtentsRefit = false;
            }
        }

        private ref Renderable CreateRenderable()
        {
            ++_entityCount;

            ref RenderableChunk chunk = ref Unsafe.NullRef<RenderableChunk>();
            int chunkIndex = -1;

            for (int i = _chunks.Count - 1; i >= 0; i--)
            {
                chunk = ref _chunks.GetRefAtIndex(i);
                if (!chunk.IsFull)
                {
                    chunkIndex = i;
                    break;
                }
            }

            if (Unsafe.IsNullRef(in chunk) || chunk.IsFull)
            {
                chunkIndex = _chunks.Count;

                _chunks.Add(new RenderableChunk(ArrayPool<Renderable>.Shared));
                chunk = ref _chunks.GetRefAtIndex(chunkIndex);
            }

            ref Renderable renderable = ref chunk.Create(out int index);
            renderable.Id = _ids.Take();

            _slots.EnsureCapacity(_entityCount);
            _slots.Add(renderable.Id, new RenderableSlot(chunkIndex, index));

            return ref renderable;
        }

        private void DestroyRenderable(int id)
        {
            --_entityCount;

            ref RenderableSlot slot = ref _slots[id];
            ref RenderableChunk chunk = ref _chunks.GetRefAtIndex(slot.Chunk);

            {
                ref Renderable renderable = ref chunk[slot.Index];
                renderable.MaterialPasses.Dispose();
            }

            if (_chunks.Count == slot.Chunk + 1)
            {
                if (chunk.Count == 1)
                {
                    chunk.Dispose();
                    _chunks.RemoveAt(slot.Chunk);
                }
                else
                {
                    chunk.Destroy(slot.Index, out int movedId);

                    if (movedId != -1)
                    {
                        _slots[movedId] = slot;
                    }
                }
            }
            else
            {
                ref RenderableChunk lastChunk = ref _chunks.GetRefAtIndex(_chunks.Count - 1);
                chunk.DestroyAndTake(slot.Index, ref lastChunk, out int movedId);

                if (movedId != -1)
                {
                    _slots[movedId] = slot;
                }

                if (lastChunk.IsEmpty)
                {
                    lastChunk.Dispose();
                    _chunks.RemoveAt(_chunks.Count - 1);
                }
            }

            _slots.Remove(id);
        }

        public ref Renderable GetRenderable(int id)
        {
            ref RenderableSlot slot = ref _slots[id];
            return ref _chunks.GetRefAtIndex(slot.Chunk)[slot.Index];
        }

        public RenderExtents SourceExtents => _sourceExtents;
        public Int3 GridPoint => _gridPoint;

        public RenderExtents CurrentExtents => _currentExtents;

        public int EntityCount => _entityCount;
 
        public bool WantsExtentsRefit => _wantsExtentsRefit;
        public bool HasUpdates => !_updates.IsEmpty;

        public ReadOnlySpan<RenderableChunk> Chunks => _chunks.AsSpan();
        public RODictionary<RenderMesh, PooledSwapAndPopList<int>> RedirectionLists => _redirectionLists;

        public bool IsEmpty => _entityCount == 0;

        public static Int3 GetGridPointFor(Vector3 position)
        {
            Vector128<float> source = position.AsVector128Unsafe();
            source /= Vector128.Create(GridPartitionSize);
            source = Vector128.Round(source);

            return Vector128.ConvertToInt32(source).AsInt3();
        }

        public static bool DoesExtendBeyondBounds(RenderExtents extents, Vector3 center, float sqrRadius)
        {
            float radius = float.Sqrt(sqrRadius);

            Vector3 diff = Vector3.Abs(center - extents.Center);
            Vector3 outside = diff - extents.HalfExtents;

            return MathF.Max(MathF.Max(outside.X, outside.Y), outside.Z) + radius > 0.0f;
        }

        public const float GridPartitionSize = 64;
        public const int MaxTreeDepth = 2;

        public const int ObjectCombineThreshold = 96;
        public const int ObjectSplitThreshold = 256;

        public const float MaxOctantLoseness = 0.5f;
    }
}
