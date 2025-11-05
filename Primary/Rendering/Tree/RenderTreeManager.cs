using Arch.Core;
using Primary.Common;
using Primary.Components;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Scenes;
using Primary.Timing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.Tree
{
    public sealed class RenderTreeManager
    {
        private Dictionary<TreeRegion, RenderTree> _treeRegions;

        private Queue<TreeUpdateData> _updatedEntities;

        internal RenderTreeManager()
        {
            _treeRegions = new Dictionary<TreeRegion, RenderTree>();

            _updatedEntities = new Queue<TreeUpdateData>();
        }

        internal void UpdateForFrame()
        {
            using (new ProfilingScope("UpdateTree"))
            {
                _updatedEntities.Clear();

                DispatchQueries();
                UpdateEntities();
            }
        }

        private void DispatchQueries()
        {
            using (new ProfilingScope("Query"))
            {
                FindMissingEntitiesJob job1 = new FindMissingEntitiesJob { Manager = this };
                FindUpdatedEntitiesJob job2 = new FindUpdatedEntitiesJob { Manager = this, FrameIndex = Time.FrameIndex };

                World world = Engine.GlobalSingleton.SceneManager.World;
                world.InlineEntityQuery<FindMissingEntitiesJob, RenderBounds>(FindMissingEntitiesJob.Query, ref job1);
                world.InlineEntityQuery<FindUpdatedEntitiesJob, RenderBounds, RenderTreeAdditionalInfo>(FindUpdatedEntitiesJob.Query, ref job2);
            }
        }

        private void UpdateEntities()
        {
            using (new ProfilingScope("Update"))
            {
                while (_updatedEntities.TryDequeue(out TreeUpdateData result))
                {
                    switch (result.UpdateType)
                    {
                        case TreeUpdateType.Created: AddEntityToTree(result.Entity); break;
                        case TreeUpdateType.MovedTree: MoveEntityOutsideTree(result.Entity); break;
                        case TreeUpdateType.MovedNode: MoveEntityWithinTree(result.Entity); break;
                    }
                }
            }
        }

        private void AddEntityToTree(SceneEntity entity)
        {
            entity.AddComponent<RenderTreeAdditionalInfo>();

            ref RenderBounds bounds = ref entity.GetComponent<RenderBounds>();
            ref RenderTreeAdditionalInfo additionalInfo = ref entity.GetComponent<RenderTreeAdditionalInfo>();
            
            Debug.Assert(!Unsafe.IsNullRef(ref bounds));
            Debug.Assert(!Unsafe.IsNullRef(ref additionalInfo));

            (TreeRegion treeRegion, TreeRegion nodeRegion) = CalculateTreeAndNodeRegion(bounds.ComputedBounds.Center);

            RenderTree tree = GetTreeInRegion(treeRegion)!;
            tree.AddEntityToTree(entity, nodeRegion);

            additionalInfo.TreeRegion = treeRegion;
            additionalInfo.NodeRegion = nodeRegion;
        }

        private void MoveEntityOutsideTree(SceneEntity entity)
        {
            ref RenderBounds bounds = ref entity.GetComponent<RenderBounds>();
            ref RenderTreeAdditionalInfo additionalInfo = ref entity.GetComponent<RenderTreeAdditionalInfo>();

            Debug.Assert(!Unsafe.IsNullRef(ref bounds));
            Debug.Assert(!Unsafe.IsNullRef(ref additionalInfo));

            (TreeRegion treeRegion, TreeRegion nodeRegion) = CalculateTreeAndNodeRegion(bounds.ComputedBounds.Center);
            if (additionalInfo.TreeRegion == treeRegion)
            {
                EngLog.Render.Warning("Entity update: {e} is invalid as its bounding box center has not moved tree.", TreeUpdateType.MovedTree);
                return;
            }

            RenderTree? oldTree = GetTreeInRegion(additionalInfo.TreeRegion, false);
            RenderTree newTree = GetTreeInRegion(treeRegion)!;

            oldTree?.RemoveEntityFromTree(entity);
            newTree.AddEntityToTree(entity, nodeRegion);

            additionalInfo.TreeRegion = treeRegion;
            additionalInfo.NodeRegion = nodeRegion;
        }

        private void MoveEntityWithinTree(SceneEntity entity)
        {
            ref RenderBounds bounds = ref entity.GetComponent<RenderBounds>();
            ref RenderTreeAdditionalInfo additionalInfo = ref entity.GetComponent<RenderTreeAdditionalInfo>();

            Debug.Assert(!Unsafe.IsNullRef(ref bounds));
            Debug.Assert(!Unsafe.IsNullRef(ref additionalInfo));

            RenderTree? tree = GetTreeInRegion(additionalInfo.TreeRegion, false);
            if (tree == null)
            {
                EngLog.Render.Warning("Entity update: {e} is invalid as its owning tree does not exist.", TreeUpdateType.MovedNode);
                return;
            }

            tree.MoveEntityNode(entity, CalculateTreeAndNodeRegion(bounds.ComputedBounds.Center).node);
        }

        private RenderTree? GetTreeInRegion(TreeRegion region, bool createIfNull = true)
        {
            if (!_treeRegions.TryGetValue(region, out RenderTree? tree))
            {
                if (!createIfNull)
                    return null;

                tree = new RenderTree(new Vector3(region.RegionX, region.RegionY, region.RegionZ) * Constants.rRenderTreeRegionSize);
                _treeRegions[region] = tree;
            }

            return tree;
        }

        public IReadOnlyDictionary<TreeRegion, RenderTree> Trees => _treeRegions;

        private static TreeRegion CalculateTreeRegion(Vector3 center)
        {
            Vector128<float> globalNodeRegion = center.AsVector128Unsafe();
            globalNodeRegion /= s_convertWorldToGlobalNode;

            Vector128<float> treeRegion = globalNodeRegion / s_convertGlobalNodeToTree;
            treeRegion = Vector128.Floor(treeRegion);

            return new TreeRegion((int)treeRegion.GetElement(0), (int)treeRegion.GetElement(1), (int)treeRegion.GetElement(2));
        }

        private static (TreeRegion tree, TreeRegion node) CalculateTreeAndNodeRegion(Vector3 center)
        {
            Vector128<float> globalNodeRegion = center.AsVector128Unsafe();
            globalNodeRegion /= s_convertWorldToGlobalNode;

            Vector128<float> treeRegion = globalNodeRegion / s_convertGlobalNodeToTree;

            globalNodeRegion = Vector128.Truncate(globalNodeRegion);
            treeRegion = Vector128.Truncate(treeRegion);

            globalNodeRegion -= treeRegion * s_convertGlobalNodeToTree;
            
            return (
                new TreeRegion((int)treeRegion.GetElement(0), (int)treeRegion.GetElement(1), (int)treeRegion.GetElement(2)),
                new TreeRegion((int)globalNodeRegion.GetElement(0), (int)globalNodeRegion.GetElement(1), (int)globalNodeRegion.GetElement(2)));
        }

        internal const float MinimumNodeSize = Constants.rRenderTreeRegionSize / (float)(Constants.rRenderTreeDepth + 1);
        internal const int SmallRegionLimit = (Constants.rRenderTreeDepth + 1);
        internal const int NodeRegionToTreeValue = Constants.rRenderTreeDepth + 1;

        private static readonly Vector128<float> s_convertWorldToGlobalNode = Vector128.Create(MinimumNodeSize, MinimumNodeSize, MinimumNodeSize, 1.0f);
        private static readonly Vector128<float> s_convertGlobalNodeToTree = Vector128.Create(NodeRegionToTreeValue, NodeRegionToTreeValue, NodeRegionToTreeValue, 1.0f);

        private struct FindMissingEntitiesJob : IForEachWithEntity<RenderBounds>
        {
            public RenderTreeManager Manager;

            public void Update(Entity entity, ref RenderBounds bounds)
            {
                Manager._updatedEntities.Enqueue(new TreeUpdateData(entity, TreeUpdateType.Created));
            }

            public static QueryDescription Query = new QueryDescription().WithAll<RenderBounds>().WithNone<RenderTreeAdditionalInfo>();
        }

        private struct FindUpdatedEntitiesJob : IForEachWithEntity<RenderBounds, RenderTreeAdditionalInfo>
        {
            public RenderTreeManager Manager;
            public int FrameIndex;

            public void Update(Entity entity, ref RenderBounds bounds, ref RenderTreeAdditionalInfo additionalInfo)
            {
                if (bounds.UpdateIndex != FrameIndex)
                    return;

                Vector3 center = bounds.ComputedBounds.Center;
                (TreeRegion treeRegion, TreeRegion nodeRegion) = CalculateTreeAndNodeRegion(center);

                if (treeRegion != additionalInfo.TreeRegion)
                {
                    Manager._updatedEntities.Enqueue(new TreeUpdateData(entity, TreeUpdateType.MovedTree));
                    return;
                }

                if (nodeRegion != additionalInfo.NodeRegion)
                {
                    Manager._updatedEntities.Enqueue(new TreeUpdateData(entity, TreeUpdateType.MovedNode));
                }
            }

            public static QueryDescription Query = new QueryDescription().WithAll<RenderBounds, RenderTreeAdditionalInfo>();
        }

        internal readonly record struct TreeUpdateData(SceneEntity Entity, TreeUpdateType UpdateType);

        internal enum TreeUpdateType : byte
        {
            Created,
            MovedTree,
            MovedNode
        }
    }

    public readonly record struct TreeRegion(int RegionX, int RegionY, int RegionZ) : IEquatable<TreeRegion>
    {
        public override int GetHashCode()
        {
            return HashCode.Combine(RegionX, RegionY, RegionZ);
        }

        public bool Equals(TreeRegion other)
        {
            return Vector128.EqualsAll(AsVector128(), other.AsVector128());
        }

        public override string ToString()
        {
            return $"{RegionX}x{RegionY}x{RegionZ}";
        }

        public Vector3 AsWorldVector3()
        {
            Vector128<int> vector;
            Unsafe.SkipInit(out vector);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref vector), this);

            vector *= Constants.rRenderTreeRegionSize;
            return vector.As<int, float>().AsVector3();
        }

        public Vector128<int> AsVector128Unsafe()
        {
            Vector128<int> vector;
            Unsafe.SkipInit(out vector);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref vector), this);

            return vector;
        }

        public Vector128<int> AsVector128()
        {
            Vector128<int> vector;
            Unsafe.SkipInit(out vector);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref vector), this);

            return vector.WithElement(3, 0);
        }
    }
}
