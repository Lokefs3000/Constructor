using Primary.Common;
using Primary.Mathematics;
using Primary.Components;
using Primary.Scenes;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Vortice.Mathematics;
using Primary.Collections.ReadOnly;

namespace Primary.Rendering.Tree
{
    public sealed class RegionOctree
    {
        private readonly AABB _worldBounds;
        private readonly OctreePoint _octreePoint;

        private readonly RenderOctant _rootOctant;
        private Dictionary<int, RenderOctant> _octantDict;

        private readonly List<SceneEntity> _children;

        internal RegionOctree(AABB worldBounds, OctreePoint octreePoint)
        {
            _worldBounds = worldBounds;
            _octreePoint = octreePoint;

            _rootOctant = new RenderOctant(null, worldBounds, worldBounds.Minimum);
            _octantDict = new Dictionary<int, RenderOctant>
            {
                { _rootOctant.OctantId, _rootOctant }
            };

            _children = new List<SceneEntity>();
        }

        internal void EmplaceWithinTree(SceneEntity entity, AABB boundaries, RenderOctant? baseOctant = null)
        {
            (RenderOctant ? fittedOctant, float oversize) = GetFittingOctant(boundaries, baseOctant ?? _rootOctant);
            if (fittedOctant != null)
            {
                fittedOctant.EmplaceChild(entity, oversize);

                ref RenderOctantInfo octantInfo = ref entity.GetComponent<RenderOctantInfo>();
                octantInfo.Tree = _octreePoint;
                octantInfo.OctantId = fittedOctant.OctantId;
            }
            else
            {
                _children.Add(entity);

                ref RenderOctantInfo octantInfo = ref entity.GetComponent<RenderOctantInfo>();
                octantInfo.Tree = _octreePoint;
                octantInfo.OctantId = IsContainedWithinRegionId;
            }
        }

        internal void RemoveFromTree(SceneEntity entity, int octantId)
        {
            if (octantId == IsContainedWithinRegionId)
            {
                _children.Remove(entity);
            }
            else
            {
                if (!_octantDict.TryGetValue(octantId, out RenderOctant? octant))
                {
                    throw new InvalidOperationException("placeholder error");
                }

                octant.RemoveChild(entity);

                RenderOctant? parentOctant = octant.Owner;
                if (parentOctant == null)
                {
                    //TODO: handle empty!
                    return;
                }

                Debug.Assert(!parentOctant.Octants.IsEmpty);

                int total = 0;
                foreach (RenderOctant subOctant in parentOctant.Octants)
                {
                    total += subOctant.ChildrenList.Count;
                    if (total > OctantEntityLimit)
                        return;
                }

                MergeOctantChildren(parentOctant);
            }
        }

        internal void MoveEntityWithinTree(SceneEntity entity, AABB boundaries, int octantId)
        {
            if (!_octantDict.TryGetValue(octantId, out RenderOctant? oldOctant))
            {
                throw new InvalidOperationException("placeholder error");
            }

            (RenderOctant? newOctant, float oversize) = GetFittingOctant(boundaries, _rootOctant);

            if (newOctant == null)
            {
                oldOctant.RemoveChild(entity);
                _children.Add(entity);

                ref RenderOctantInfo octantInfo = ref entity.GetComponent<RenderOctantInfo>();
                octantInfo.OctantId = IsContainedWithinRegionId;
            }
            else if (oldOctant.Owner == newOctant.Owner)
            {
                oldOctant.RemoveChild(entity);
                newOctant.EmplaceChild(entity, oversize);

                ref RenderOctantInfo octantInfo = ref entity.GetComponent<RenderOctantInfo>();
                octantInfo.OctantId = newOctant.OctantId;
            }
            else
            {
                RemoveFromTree(entity, octantId);
                EmplaceWithinTree(entity, boundaries);
            }
        }

        private (RenderOctant? Octant, float Oversize) GetFittingOctant(AABB boundaries, RenderOctant octant)
        {
            // initial test
            {
                float largest = FindExtentsOutOfBounds(octant.Boundaries, boundaries);
                if (largest > ScaledMaxExtents / octant.Depth)
                {
                    return (null, 0.0f);
                }
            }

            RenderOctant? previousFittingOctant = octant;
            do
            {
                OctreePoint point = GetOctreePointFor(boundaries.Center - octant.Boundaries.Minimum, octant.Boundaries.Size * 0.5f);

                RenderOctant? subOctant = octant.GetOctantAt(point);
                if (subOctant == null && octant.Depth == 0)
                    subOctant = octant;
                Debug.Assert(subOctant != null);

                float largest = FindExtentsOutOfBounds(subOctant.Boundaries, boundaries);
                if (largest > ScaledMaxExtents / octant.Depth)
                {
                    return (previousFittingOctant, largest);
                }
                else if (largest > 0.0f)
                {
                    // It will be out of bounds for any octant smaller
                    return (octant, largest);
                }

                previousFittingOctant = octant;
                octant = subOctant;

                if (subOctant.Children.Count >= OctantEntityLimit)
                {
                    SplitOctantAndChildren(subOctant);
                }
            } while (!octant.Octants.IsEmpty);

            return (octant, 0.0f);
        }

        private void SplitOctantAndChildren(RenderOctant octant)
        {
            Debug.Assert(octant.OctantsList == null);

            using RentedArray<SceneEntity> children = RentedArray<SceneEntity>.Rent(octant.Children.Count);

            octant.ChildrenList.CopyTo(children.Span);
            octant.ChildrenList.Clear();

            Vector3 size = octant.Boundaries.Size * 0.5f;
            AABB smallBounds = new AABB(octant.Boundaries.Minimum, octant.Boundaries.Minimum + size);

            octant.OctantsList = [
                new RenderOctant(octant, smallBounds, _worldBounds.Minimum), //Bottom left back,
                new RenderOctant(octant, AABB.Offset(smallBounds, new Vector3(size.X, 0.0f, 0.0f)), _worldBounds.Minimum), //Bottom right back,
                new RenderOctant(octant, AABB.Offset(smallBounds, new Vector3(0.0f, size.Y, 0.0f)), _worldBounds.Minimum), //Top left back,
                new RenderOctant(octant, AABB.Offset(smallBounds, new Vector3(size.X, size.Y, 0.0f)), _worldBounds.Minimum), //Top right back,

                new RenderOctant(octant, AABB.Offset(smallBounds, new Vector3(0.0f, 0.0f, size.Z)), _worldBounds.Minimum), //Bottom left front
                new RenderOctant(octant, AABB.Offset(smallBounds, new Vector3(size.X, 0.0f, size.Z)), _worldBounds.Minimum), //Bottom right front
                new RenderOctant(octant, AABB.Offset(smallBounds, new Vector3(0.0f, size.Y, size.Z)), _worldBounds.Minimum), //Top left front
                new RenderOctant(octant, AABB.Offset(smallBounds, new Vector3(size.X, size.Y, size.Z)), _worldBounds.Minimum), //Top right front
                ];

            for (var i = 0; i < 8; i++)
            {
                _octantDict.Add(octant.OctantsList[i].OctantId, octant.OctantsList[i]);
            }

            if (children.Count > 0)
            {
                foreach (SceneEntity child in children)
                {
                    ref RenderBounds bounds = ref child.GetComponent<RenderBounds>();
                    Debug.Assert(!Unsafe.IsNullRef(ref bounds));

                    EmplaceWithinTree(child, bounds.ComputedBounds, octant);
                }
            }
        }

        private void MergeOctantChildren(RenderOctant octant)
        {
            Debug.Assert(!octant.Octants.IsEmpty);
            foreach (RenderOctant subOctant in octant.Octants)
            {
                octant.ChildrenList.AddRange(subOctant.ChildrenList);
                _octantDict.Remove(subOctant.OctantId); //TODO: consider validating this?
            }

            foreach (SceneEntity entity in octant.Children)
            {
                ref RenderOctantInfo octantInfo = ref entity.GetComponent<RenderOctantInfo>();
                ref RenderBounds renderBounds = ref entity.GetComponent<RenderBounds>();
                octantInfo.OctantId = octant.OctantId;

                float oversize = FindExtentsOutOfBounds(_worldBounds, renderBounds.ComputedBounds);
                if (oversize > 0.0f)
                {
                    octant.OversizeInternalBoundaries(renderBounds.ComputedBounds);
                }
            }

            octant.OctantsList = null;
        }

        public AABB WorldBounds => _worldBounds;
        public OctreePoint Point => _octreePoint;

        public RenderOctant RootOctant => _rootOctant;

        public ROList<SceneEntity> Children => _children;

        private static float FindExtentsOutOfBounds(AABB octant, AABB entity)
        {
            Vector128<float> minExtent = entity.Minimum.AsVector128Unsafe() - octant.Minimum.AsVector128Unsafe();
            Vector128<float> maxExtent = entity.Maximum.AsVector128Unsafe() - octant.Maximum.AsVector128Unsafe();

            Vector128<float> large = Vector128.Max(Vector128.Max(minExtent, maxExtent), Vector128<float>.Zero);

            return Math.Max(Math.Max(large[0], large[1]), large[2]) / (octant.Maximum.X - octant.Minimum.X);
        }

        private static OctreePoint GetOctreePointFor(Vector3 position, Vector3 octantSize)
        {
            Vector128<float> center = position.AsVector128Unsafe();
            center = Vector128.Divide(center, octantSize.AsVector128Unsafe());
            center = Vector128.Truncate(center); //duplicate/unnecesary?

            return new OctreePoint((int)center.GetX(), (int)center.GetY(), (int)center.GetZ());
        }

        public const int OctantEntityLimit = 64;
        public const int MaxOctantTreeDepth = 4;
        public const int OctreeRegionSize = 128;
        public const float ScaledMaxExtents = 0.3f;

        public const int IsContainedWithinRegionId = int.MinValue;
    }
}
