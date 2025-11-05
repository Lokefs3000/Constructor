using Arch.Core;
using Primary.Common;
using Primary.Components;
using Primary.Rendering2.Components;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using Vortice.Mathematics;

namespace Primary.Rendering2.Tree
{
    public sealed class RegionOctree
    {
        private readonly AABB _worldBounds;
        private readonly OctreePoint _octreePoint;

        private readonly RenderOctant _rootOctant;
        private Dictionary<int, RenderOctant> _octantDict;

        internal RegionOctree(AABB worldBounds, OctreePoint octreePoint)
        {
            _worldBounds = worldBounds;
            _octreePoint = octreePoint;

            _rootOctant = new RenderOctant(null, worldBounds, worldBounds.Minimum);
            _octantDict = new Dictionary<int, RenderOctant>
            {
                { _rootOctant.OctantId, _rootOctant }
            };
        }

        internal void EmplaceWithinTree(SceneEntity entity, AABB boundaries, RenderOctant? baseOctant = null)
        {
            RenderOctant fittedOctant = GetFittingOctant(boundaries, baseOctant ?? _rootOctant);
            fittedOctant.EmplaceChild(entity);

            ref RenderOctantInfo octantInfo = ref entity.GetComponent<RenderOctantInfo>();
            octantInfo.Tree = _octreePoint;
            octantInfo.OctantId = fittedOctant.OctantId;
        }

        internal void RemoveFromTree(SceneEntity entity, int octantId)
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

        internal void MoveEntityWithinTree(SceneEntity entity, AABB boundaries, int octantId)
        {
            if (!_octantDict.TryGetValue(octantId, out RenderOctant? oldOctant))
            {
                throw new InvalidOperationException("placeholder error");
            }

            RenderOctant newOctant = GetFittingOctant(boundaries, _rootOctant);

            if (oldOctant.Owner == newOctant.Owner)
            {
                oldOctant.RemoveChild(entity);
                newOctant.EmplaceChild(entity);

                ref RenderOctantInfo octantInfo = ref entity.GetComponent<RenderOctantInfo>();
                octantInfo.OctantId = newOctant.OctantId;
            }
            else
            {
                RemoveFromTree(entity, octantId);
                EmplaceWithinTree(entity, boundaries);
            }
        }

        private RenderOctant GetFittingOctant(AABB bounadries, RenderOctant octant)
        {
            int depth = 0;

            do
            {
                OctreePoint point = GetOctreePointFor(bounadries.Center - octant.Boundaries.Minimum, octant.Boundaries.Size * 0.5f);

                RenderOctant? subOctant = octant.GetOctantAt(point);
                if (subOctant == null && depth == 0)
                    subOctant = octant;
                Debug.Assert(subOctant != null);

                float largest = FindExtentsOutOfBounds(subOctant.Boundaries, bounadries);
                if (largest > ScaledMaxExtents / (float)depth)
                {
                    return octant;
                }

                octant = subOctant;
                depth++;

                if (subOctant.Children.Count >= OctantEntityLimit)
                {
                    SplitOctantAndChildren(subOctant);
                }
            } while (!octant.Octants.IsEmpty);

            return octant;
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
                octantInfo.OctantId = octant.OctantId;
            }

            octant.OctantsList = null;
        }

        internal AABB WorldBounds => _worldBounds;
        internal OctreePoint Point => _octreePoint;

        internal RenderOctant RootOctant => _rootOctant;

        private static float FindExtentsOutOfBounds(AABB octant, AABB entity)
        {
            return 0.0f;
            Vector128<float> minExtent = Vector128.Subtract(entity.Minimum.AsVector128Unsafe(), octant.Minimum.AsVector128Unsafe());
            Vector128<float> maxExtent = Vector128.Subtract(
                Vector128.Abs(octant.Maximum.AsVector128Unsafe()),
                Vector128.Abs(entity.Minimum.AsVector128Unsafe()));

            Vector128<float> large = Vector128.Max(minExtent, maxExtent);

            return MathF.Max(MathF.Max(large.GetX(), large.GetY()), large.GetZ());
        }

        private static OctreePoint GetOctreePointFor(Vector3 position, Vector3 octantSize)
        {
            Vector128<float> center = position.AsVector128Unsafe();
            center = Vector128.Divide(center, octantSize.AsVector128Unsafe());
            center = Vector128.Truncate(center); //duplicate/unnecesary?

            return new OctreePoint((int)center.GetX(), (int)center.GetY(), (int)center.GetZ());
        }

        public const int OctantEntityLimit = 128;
        public const int MaxOctantTreeDepth = 4;
        public const int OctreeRegionSize = 128;
        public const float ScaledMaxExtents = 0.3f;
    }
}
