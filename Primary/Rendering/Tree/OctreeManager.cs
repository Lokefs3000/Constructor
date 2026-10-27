using Arch.Core;
using Primary.Common;
using Primary.Mathematics;
using Primary.Components;
using Primary.Profiling;
using Primary.Scenes;
using Primary.Scenes.Components;
using Primary.Timing;
using System.Numerics;
using System.Runtime.Intrinsics;

using SceneEntityManager = Primary.Scenes.Components.SceneEntityManager;

namespace Primary.Rendering.Tree
{
    public sealed class OctreeManager
    {
        private Dictionary<OctreePoint, RegionOctree> _regions;
        private Queue<EntityUpdateData> _pendingUpdates;

        internal OctreeManager()
        {
            _regions = new Dictionary<OctreePoint, RegionOctree>();
            _pendingUpdates = new Queue<EntityUpdateData>();

            SceneEntityManager.Instance.AddComponentAddedCallback<RenderBounds>((e) =>
            {
                _pendingUpdates.Enqueue(new EntityUpdateData(EntityUpdateType.Created, e));
            });

            SceneEntityManager.Instance.AddComponentRemovedCallback<RenderBounds>((e) =>
            {
                _pendingUpdates.Enqueue(new EntityUpdateData(EntityUpdateType.Removed, e));
            });

            SceneEntityManager.Events |= EntityEvents.EntityComponents;
        }

        internal void QueryPending()
        {
            using (new ProfilingScope("OctreeUpdate"))
            {
                World world = Engine.GlobalSingleton.SceneManager.World;

                if (_pendingUpdates.Count > 0)
                {
                    while (_pendingUpdates.TryDequeue(out EntityUpdateData updateData))
                    {
                        switch (updateData.Type)
                        {
                            case EntityUpdateType.Created:
                                {
                                    if (updateData.Entity.IsNull)
                                        continue;

                                    ref RenderOctantInfo octantInfo = ref updateData.Entity.AddComponent<RenderOctantInfo>();
                                    ref RenderBounds bounds = ref updateData.Entity.GetComponent<RenderBounds>();

                                    OctreePoint point = GetOctreeRegionForPoint(bounds.ComputedBounds.Center);
                                    RegionOctree octree = GetOrCreateOctree(point);

                                    octree.EmplaceWithinTree(updateData.Entity, bounds.ComputedBounds);
                                    break;
                                }
                            case EntityUpdateType.Removed: throw new NotImplementedException();
                        }
                    }
                }

                QueryUpdatedBoundsJob job = new QueryUpdatedBoundsJob(this);
                world.InlineEntityQuery<QueryUpdatedBoundsJob, RenderBounds, RenderOctantInfo>(QueryUpdatedBoundsJob.Query, ref job);
            }
        }

        private void MoveEntityTreeWise(SceneEntity entity, OctreePoint newPoint)
        {
            ref RenderBounds bounds = ref entity.GetComponent<RenderBounds>();
            ref RenderOctantInfo octantInfo = ref entity.GetComponent<RenderOctantInfo>();

            RegionOctree oldOctree = GetOrCreateOctree(octantInfo.Tree);
            RegionOctree newOctree = GetOrCreateOctree(newPoint);

            oldOctree.RemoveFromTree(entity, octantInfo.OctantId);
            newOctree.EmplaceWithinTree(entity, bounds.ComputedBounds);
        }

        private void MoveEntityOctantWise(SceneEntity entity, OctreePoint point)
        {
            ref RenderBounds bounds = ref entity.GetComponent<RenderBounds>();
            ref RenderOctantInfo octantInfo = ref entity.GetComponent<RenderOctantInfo>();

            RegionOctree octree = GetOrCreateOctree(point);
            octree.MoveEntityWithinTree(entity, bounds.ComputedBounds, octantInfo.OctantId);
        }

        private RegionOctree GetOrCreateOctree(OctreePoint point)
        {
            if (!_regions.TryGetValue(point, out RegionOctree? octree))
            {
                Vector3 basePosition = new Vector3(point.X, point.Y, point.Z) * RegionOctree.OctreeRegionSize;
                octree = new RegionOctree(new AABB(basePosition, basePosition + new Vector3(RegionOctree.OctreeRegionSize)), point);

                _regions.Add(point, octree);
            }

            return octree;
        }

        public Dictionary<OctreePoint, RegionOctree> Regions => _regions;

        public static OctreePoint GetOctreeRegionForPoint(Vector3 position)
        {
            Vector128<float> vector = Vector128.Floor(position.AsVector128Unsafe() / RegionOctree.OctreeRegionSize);
            Vector128<int> vectorInt = Vector128.ConvertToInt32(vector);

            //prob dont need "Unsafe.WriteUnaligned" as it should be so fast it wont matter
            return new OctreePoint((short)vectorInt.GetElement(0), (short)vectorInt.GetElement(1), (short)vectorInt.GetElement(2));
        }

        private readonly record struct EntityUpdateData(EntityUpdateType Type, SceneEntity Entity);

        private enum EntityUpdateType : byte
        {
            Created,
            Removed
        }

        private readonly record struct QueryUpdatedBoundsJob : IForEachWithEntity<RenderBounds, RenderOctantInfo>
        {
            public readonly OctreeManager Manager;
            public readonly int FrameIndex;

            public QueryUpdatedBoundsJob(OctreeManager manager)
            {
                Manager = manager;
                FrameIndex = Time.FrameIndex;
            }

            public void Update(Entity entity, ref RenderBounds bounds, ref RenderOctantInfo octantInfo)
            {
                if (FrameIndex == bounds.UpdateIndex)
                {
                    OctreePoint point = GetOctreeRegionForPoint(bounds.ComputedBounds.Center);
                    if (point != octantInfo.Tree && octantInfo.OctantId.Code != RegionOctree.NullOctantId)
                    {
                        Manager.MoveEntityTreeWise(entity, point);
                        return;
                    }

                    RegionOctree octree = Manager.GetOrCreateOctree(point);
                    OctreePoint octantPoint = RenderOctant.GetOctreePointForOctant(bounds.ComputedBounds.Center - octree.WorldBounds.Minimum);
                    int octantId = RenderOctant.CraftIdFromPoint(octantPoint);
                    if (octantId != octantInfo.OctantId.Code)
                    {
                        Manager.MoveEntityOctantWise(entity, point);
                        return;
                    }
                }
            }

            public static readonly QueryDescription Query = new QueryDescription().WithAll<RenderBounds, RenderOctantInfo>();
        }
    }

    public readonly record struct OctreePoint(short X, short Y, short Z) : IEquatable<OctreePoint>
    {
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"{X}x{Y}x{Z}";
    }

    public readonly record struct OctreeId(int Code, int HintId = -1) : IEquatable<OctreeId>
    {
        public override int GetHashCode() => HashCode.Combine(Code, HintId);
        public override string ToString() => $"{Code}[{HintId}]";
    }
}
