using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using Primary.Mathematics;

namespace Primary.Rendering.Tree
{
    public sealed class RenderOctree
    {
        private readonly RenderExtents _sourceExtents;
        private readonly Int3 _gridPoint;

        private RenderExtents _currentExtents;

        private RenderOctant[]? _octants;
        private int _childrenEntityCount;
        private int _entityCount;

        private byte _entityIndexCount;

        private bool _wantsInternalUpdate;
        private bool _wantsExtentsRefit;

        private readonly EntityChunkMap _entities;

        internal RenderOctree(ArrayPool<RenderableEntity> entityArrayPool, Int3 gridPoint)
        {
            _sourceExtents = new RenderExtents(gridPoint.AsVector3() * GridPartitionSize, new Vector3(GridPartitionSize / 2));
            _gridPoint = gridPoint;

            _currentExtents = _sourceExtents;

            _octants = null;
            _childrenEntityCount = 0;
            _entityCount = 0;

            _entityIndexCount = 1;

            _wantsInternalUpdate = false;
            _wantsExtentsRefit = false;

            _entities = new EntityChunkMap(entityArrayPool);
        }

        internal ref RenderableEntity CreateRenderable(out int id)
        {
            ref RenderableEntity entity = ref _entities.CreateRenderableEntity(out id);

            int incrementCount = IncrementCount();
            if (incrementCount > ObjectSplitThreshold && _octants == null)
                _wantsInternalUpdate = true;

            return ref entity;
        }

        internal void DestroyRenderable(int id)
        {
            ref RenderableEntity entity = ref _entities.GetEntity(id);

            int decrementCount = (entity.Octant == null) ? DecrementCount() : entity.Octant.DecrementCount();
            if (decrementCount < ObjectCombineThreshold && _octants != null)
                _wantsInternalUpdate = true;

            _entities.DestroyRenderableEntity(id);
        }

        internal ref RenderableEntity GetRenderable(int id)
        {
            return ref _entities.GetEntity(id);
        }

        internal void SetOctant(ref RenderableEntity entity, RenderOctant? octant)
        {
            if (entity.Octant == octant)
                return;

            int decrementCount = (entity.Octant == null) ? DecrementCount() : entity.Octant.DecrementCount();
            int incrementCount = (octant == null) ? IncrementCount() : octant.IncrementCount();

            if (decrementCount < ObjectCombineThreshold && (entity.Octant == null ? _octants != null : !entity.Octant.IsLeaf))
                _wantsInternalUpdate = true;
            if (incrementCount > ObjectSplitThreshold && (octant == null ? _octants == null : octant.TreeDepth < MaxTreeDepth && octant.IsLeaf))
                _wantsInternalUpdate = true;

            entity.Octant = octant;

            RenderOctant? parentOctant = octant?.Parent;
            if (parentOctant != null)
            {
                entity.OctantIndex = parentOctant.EntityIndex;
                entity.OctantBitshift = octant!.EntityBitShift;
            }
            else
            {
                entity.OctantIndex = 0;
                entity.OctantBitshift = octant?.EntityBitShift ?? 0;
            }

            _wantsExtentsRefit = true;
        }

        internal RenderOctant? FitExtentsInTree(RenderExtents extents, RenderOctant? startingOctant = null)
        {
            if ((startingOctant?.Octants ?? _octants) == null)
                return null;

            RenderOctant? currentOctant = startingOctant;

            RenderExtents sourceExtents = currentOctant?.SourceExtents ?? _sourceExtents;
            RenderOctant[] octants = currentOctant?.Octants ?? _octants!;

            while (true)
            {
                Vector128<float> offsetFromCenter = extents.Center.AsVector128Unsafe() - sourceExtents.Center.AsVector128Unsafe();
                offsetFromCenter /= Vector128.Create(extents.HalfExtents.X);
                offsetFromCenter = Vector128.Ceiling(offsetFromCenter);
                Vector128<int> indices = Vector128.ConvertToInt32(offsetFromCenter);

                RenderOctant intersectingOctant = octants[indices[0] + indices[1] * 2 + indices[3] * 4];
                if (!intersectingOctant.DoRenderExtentsFit(extents))
                {
                    return currentOctant;
                }
                else if (intersectingOctant.IsLeaf)
                {
                    return intersectingOctant;
                }

                currentOctant = intersectingOctant;

                sourceExtents = intersectingOctant.SourceExtents;
                octants = intersectingOctant.Octants;
            }
        }

        internal RenderOctant? FitExtentsInParentTree(RenderExtents extents, RenderOctant startingOctant)
        {
            RenderOctant? currentOctant = startingOctant;
            while (currentOctant != null && !currentOctant.DoRenderExtentsFit(extents))
            {
                currentOctant = currentOctant.Parent;
            }

            return currentOctant;
        }

        internal void UpdateInternals()
        {
            int entityCount = _entityCount + _childrenEntityCount;
            if (entityCount > ObjectSplitThreshold)
            {
                if (_octants == null)
                {
                    SplitOctants(null, out _octants);
                }
            }
            else if (entityCount < ObjectCombineThreshold)
            {
                CombineOctants(null);
            }

            if (_octants != null)
            {
                foreach (RenderOctant subOctant in _octants)
                {
                    UpdateOctant(subOctant);
                }
            }

            _wantsInternalUpdate = false;

            void UpdateOctant(RenderOctant octant)
            {
                int entityCount = octant.EntityCount + octant.ChildrenEntityCount;
                if (entityCount > ObjectSplitThreshold)
                {
                    if (octant.Octants == null)
                    {
                        SplitOctants(octant, out RenderOctant[] octants);
                        octant.Octants = octants;
                    }
                }
                else if (entityCount < ObjectCombineThreshold)
                {
                    CombineOctants(octant);
                }

                if (octant.Octants != null)
                {
                    foreach (RenderOctant subOctant in octant.Octants)
                    {
                        UpdateOctant(subOctant);
                    }
                }
            }
        }

        internal void SplitOctants(RenderOctant? parentOctant, out RenderOctant[] octants)
        {
            RenderExtents extents = parentOctant?.SourceExtents ?? _sourceExtents;
            byte parentOctantIndex = parentOctant?.EntityIndex ?? 0;

            Vector3 newExtents = extents.HalfExtents * 0.5f;
            byte entityIndex = _entityIndexCount++;

            Debug.Assert(_entityIndexCount > entityIndex);

            octants = [
                new RenderOctant(this, new RenderExtents(extents.Center + new Vector3(-newExtents.X, -newExtents.Y, -newExtents.Z), newExtents), parentOctant, entityIndex, 0),     //X- Y- Z-
                new RenderOctant(this, new RenderExtents(extents.Center + new Vector3(+newExtents.X, -newExtents.Y, -newExtents.Z), newExtents), parentOctant, entityIndex, 2),     //X+ Y- Z-
                new RenderOctant(this, new RenderExtents(extents.Center + new Vector3(-newExtents.X, +newExtents.Y, -newExtents.Z), newExtents), parentOctant, entityIndex, 4),     //X- Y+ Z-
                new RenderOctant(this, new RenderExtents(extents.Center + new Vector3(+newExtents.X, +newExtents.Y, -newExtents.Z), newExtents), parentOctant, entityIndex, 6),     //X+ Y+ Z-
                
                new RenderOctant(this, new RenderExtents(extents.Center + new Vector3(-newExtents.X, -newExtents.Y, +newExtents.Z), newExtents), parentOctant, entityIndex, 8),     //X- Y- Z+
                new RenderOctant(this, new RenderExtents(extents.Center + new Vector3(+newExtents.X, -newExtents.Y, +newExtents.Z), newExtents), parentOctant, entityIndex, 10),    //X+ Y- Z+
                new RenderOctant(this, new RenderExtents(extents.Center + new Vector3(-newExtents.X, +newExtents.Y, +newExtents.Z), newExtents), parentOctant, entityIndex, 12),    //X- Y+ Z+
                new RenderOctant(this, new RenderExtents(extents.Center + new Vector3(+newExtents.X, +newExtents.Y, +newExtents.Z), newExtents), parentOctant, entityIndex, 14),    //X+ Y+ Z+
                ];

            if (parentOctant != null)
            {
                Debug.Assert(parentOctant.ChildrenEntityCount == 0);

                parentOctant.ChildrenEntityCount = 0;
                parentOctant.EntityCount = 0;

                foreach (ref EntityChunk chunk in _entities.Chunks)
                {
                    foreach (ref RenderableEntity entity in chunk.Entities[..chunk.Count])
                    {
                        if (entity.Octant == parentOctant)
                        {
                            float excessSpace = CalculateExcessSpace(extents, entity.Extents);
                            if (excessSpace < MaxOctantLoseness * 0.5f)
                            {
                                Vector128<float> offsetFromCenter = entity.Extents.Center.AsVector128Unsafe() - extents.Center.AsVector128Unsafe();
                                offsetFromCenter /= Vector128.Create(extents.HalfExtents.X + extents.HalfExtents.X);
                                offsetFromCenter = Vector128.Ceiling(offsetFromCenter);
                                Vector128<int> indices = Vector128.ConvertToInt32(offsetFromCenter);

                                RenderOctant currentOctant = octants[indices[0] + indices[1] * 2 + indices[3] * 4];

                                entity.Octant = currentOctant;
                                entity.OctantIndex = parentOctantIndex;
                                entity.OctantBitshift = currentOctant.EntityBitShift;

                                currentOctant.IncrementCountRaw();
                                ++parentOctant.ChildrenEntityCount;
                            }
                            else
                            {
                                ++parentOctant.EntityCount;
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.Assert(_childrenEntityCount == 0);

                _childrenEntityCount = 0;
                _entityCount = 0;

                foreach (ref EntityChunk chunk in _entities.Chunks)
                {
                    foreach (ref RenderableEntity entity in chunk.Entities[..chunk.Count])
                    {
                        if (entity.Octant == null)
                        {
                            float excessSpace = CalculateExcessSpace(extents, entity.Extents);
                            if (excessSpace < MaxOctantLoseness * 0.5f)
                            {
                                Vector128<float> offsetFromCenter = entity.Extents.Center.AsVector128Unsafe() - extents.Center.AsVector128Unsafe();
                                offsetFromCenter /= Vector128.Create(extents.HalfExtents.X + extents.HalfExtents.X);
                                offsetFromCenter = Vector128.Ceiling(offsetFromCenter);
                                Vector128<int> indices = Vector128.ConvertToInt32(offsetFromCenter);

                                RenderOctant currentOctant = octants[indices[0] + indices[1] * 2 + indices[3] * 4];
                                
                                entity.Octant = currentOctant;
                                entity.OctantIndex = parentOctantIndex;
                                entity.OctantBitshift = currentOctant.EntityBitShift;

                                currentOctant.IncrementCountRaw();
                                ++_childrenEntityCount;
                            }
                            else
                            {
                                ++_entityCount;
                            }
                        }
                    }
                }
            }

            _wantsExtentsRefit = true;
        }

        internal void CombineOctants(RenderOctant? parentOctant)
        {

        }

        internal void RefitExtents()
        {
            foreach (ref EntityChunk chunk in _entities.Chunks)
            {
                foreach (ref RenderableEntity entity in chunk.Entities[..chunk.Count])
                {
                    RenderExtents sourceExtents = entity.Octant?.CurrentExtents ?? _currentExtents;
                    RenderExtents extents = entity.Extents;

                    Vector3 offset = Vector3.Max(-Vector3.Abs(extents.Center - sourceExtents.Center) + extents.HalfExtents - sourceExtents.HalfExtents, Vector3.Zero);
                    if (offset != Vector3.Zero)
                    {
                        if (entity.Octant == null)
                        {
                            _currentExtents = new RenderExtents(
                                sourceExtents.Center,
                                Vector3.Max(sourceExtents.HalfExtents, sourceExtents.HalfExtents + offset));
                        }
                        else
                        {
                            entity.Octant.CurrentExtents = new RenderExtents(
                              sourceExtents.Center,
                              Vector3.Max(sourceExtents.HalfExtents, sourceExtents.HalfExtents + offset));
                        }
                    }
                }
            }

            _wantsExtentsRefit = false;
        }

        private int IncrementCount() => Interlocked.Increment(ref _entityCount);
        private int DecrementCount() => Interlocked.Decrement(ref _entityCount);

        internal int IncrementChildCount() => Interlocked.Increment(ref _childrenEntityCount);
        internal int DecrementChildCount() => Interlocked.Decrement(ref _childrenEntityCount);

        public RenderExtents SourceExtents => _sourceExtents;
        public Int3 GridPoint => _gridPoint;

        public RenderExtents CurrentExtents => _currentExtents;

        internal RenderOctant[]? Octants => _octants;
        public int EntityCount => _entityCount;
        public int ChildrenEntityCount => _childrenEntityCount;

        public bool WantsInternalUpdate => _wantsInternalUpdate;
        public bool WantsExtentsRefit => _wantsExtentsRefit;

        public ReadOnlySpan<EntityChunk> Chunks => _entities.Chunks;

        public static Int3 GetGridPointFor(Vector3 position)
        {
            Vector128<float> source = position.AsVector128Unsafe();
            source /= Vector128.Create(GridPartitionSize);
            source = Vector128.Round(source);

            return Vector128.ConvertToInt32(source).AsInt3();
        }

        public static float CalculateExcessSpace(RenderExtents source, RenderExtents extents)
        {
            Vector3 offset = -Vector3.Abs(extents.Center - source.Center) + extents.HalfExtents - source.HalfExtents;

            if (offset.X > offset.Y && offset.X > offset.Z)
                return offset.X / (source.HalfExtents.X + source.HalfExtents.X);
            else if (offset.X > offset.Y && offset.X > offset.Z)
                return offset.Y / (source.HalfExtents.Y + source.HalfExtents.Y);
            else
                return offset.Z / (source.HalfExtents.Z + source.HalfExtents.Z);
        }

        public const float GridPartitionSize = 128;
        public const int MaxTreeDepth = 2;

        public const int ObjectCombineThreshold = 96;
        public const int ObjectSplitThreshold = 256;

        public const float MaxOctantLoseness = 0.5f;
    }
}
