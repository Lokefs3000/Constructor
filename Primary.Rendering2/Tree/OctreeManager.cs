using Arch.Core;
using Primary.Common;
using Primary.Components;
using Primary.Profiling;
using Primary.Rendering2.Components;
using Primary.Scenes;
using Primary.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Tree
{
    public sealed class OctreeManager
    {
        private Dictionary<OctreePoint, RegionOctree> _regions;
        private Queue<EntityUpdateData> _pendingUpdates;

        internal OctreeManager()
        {
            _regions = new Dictionary<OctreePoint, RegionOctree>();
            _pendingUpdates = new Queue<EntityUpdateData>();

            SceneEntityManager.AddComponentAddedCallback<RenderBounds>((e) =>
            {
                _pendingUpdates.Enqueue(new EntityUpdateData(EntityUpdateType.Created, e));
            });

            SceneEntityManager.AddComponentRemovedCallback<RenderBounds>((e) =>
            {
                _pendingUpdates.Enqueue(new EntityUpdateData(EntityUpdateType.Removed, e));
            });
        }

        internal void QueryPending()
        {
            using (new ProfilingScope("OctreeUpdate"))
            {
                World world = Engine.GlobalSingleton.SceneManager.World;

                QueryUpdatedBoundsJob job = new QueryUpdatedBoundsJob(this);
                world.InlineEntityQuery<QueryUpdatedBoundsJob, RenderBounds, RenderOctantInfo>(QueryUpdatedBoundsJob.Query, ref job);

                if (_pendingUpdates.Count > 0)
                {
                    while (_pendingUpdates.TryDequeue(out EntityUpdateData updateData))
                    {
                        switch (updateData.Type)
                        {
                            case EntityUpdateType.Created:
                                {
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

        internal IReadOnlyDictionary<OctreePoint, RegionOctree> Regions => _regions;

        public static OctreePoint GetOctreeRegionForPoint(Vector3 position)
        {
            Vector128<float> vector = Vector128.Floor(position.AsVector128Unsafe() / RegionOctree.OctreeRegionSize);
            Vector128<int> vectorInt = Vector128.ConvertToInt32(vector);

            //prob dont need "Unsafe.WriteUnaligned" as it should be so fast it wont matter
            return new OctreePoint(vectorInt.GetElement(0), vectorInt.GetElement(1), vectorInt.GetElement(2));
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
                    if (point != octantInfo.Tree)
                    {
                        Manager.MoveEntityTreeWise(entity, point);
                        return;
                    }

                    RegionOctree octree = Manager.GetOrCreateOctree(point);
                    int octantId = RenderOctant.CraftIdFromPoint(RenderOctant.GetOctreePointForOctant(bounds.ComputedBounds.Minimum - octree.WorldBounds.Minimum));
                    if (octantId != octantInfo.OctantId)
                    {
                        Manager.MoveEntityOctantWise(entity, point);
                        return;
                    }
                }
            }

            public static readonly QueryDescription Query = new QueryDescription().WithAll<RenderBounds, RenderOctantInfo>();
        }
    }

    public readonly record struct OctreePoint(int X, int Y, int Z);
}
