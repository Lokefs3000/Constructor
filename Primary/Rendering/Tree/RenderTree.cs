using Primary.Common;
using Primary.Components;
using Primary.Mathematics;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering.Tree
{
    public sealed class RenderTree
    {
        private readonly Vector3 _rootPosition;
        private List<RenderTreeNodeData> _nodes;

        private Dictionary<TreeRegion, int> _childNodeIndices;

        private int _containedEntityCount;

        internal RenderTree(Vector3 basePosition)
        {
            _rootPosition = basePosition;
            _nodes = new List<RenderTreeNodeData>();

            _childNodeIndices = new Dictionary<TreeRegion, int>();

            _containedEntityCount = 0;

            RenderTreeNodeData rootNode = new RenderTreeNodeData(new RenderTreeNode(null, new AABB(Vector3.Zero, new Vector3(Constants.rRenderTreeRegionSize)), false), new int[8], false);
            _nodes.Add(rootNode);

            GenerateNode(rootNode.Node, rootNode.Subnodes, 1);
        }

        private void GenerateNode(RenderTreeNode parent, int[] parentIndices, int depth)
        {
            bool isLeaf = depth >= Constants.rRenderTreeDepth;

            RenderTreeNode[] split = GenerateNodeSplit(parent.Boundaries, parent, isLeaf);
            for (int i = 0; i < split.Length; i++)
            {
                int idx = _nodes.Count;

                _nodes.Add(new RenderTreeNodeData(split[i], isLeaf ? Array.Empty<int>() : new int[8], isLeaf));
                parentIndices[i] = idx;
            }

            if (!isLeaf)
            {
                depth++;
                for (int i = 0; i < split.Length; i++)
                {
                    GenerateNode(split[i], _nodes[parentIndices[i]].Subnodes, depth);
                }
            }
            else
            {
                for (int i = 0; i < split.Length; i++)
                {
                    Debug.Assert(split[i].Boundaries.Size == new Vector3(RenderTreeManager.MinimumNodeSize));

                    Vector3 vectorRegion = Vector3Ext.Ceiling(split[i].Boundaries.Minimum / RenderTreeManager.MinimumNodeSize);
                    TreeRegion region = new TreeRegion((int)vectorRegion.X, (int)vectorRegion.Y, (int)vectorRegion.Z);

                    _childNodeIndices.Add(region, parentIndices[i]);
                }
            }
        }

        private RenderTreeNode[] GenerateNodeSplit(AABB boundaries, RenderTreeNode treeNode, bool hasChildLists)
        {
            Vector3 halfSizeSplit = boundaries.Size * 0.5f;
            AABB smallBounds = new AABB(boundaries.Minimum, boundaries.Minimum + halfSizeSplit);

            RenderTreeNode ftl = new RenderTreeNode(treeNode, smallBounds, hasChildLists);
            RenderTreeNode ftr = new RenderTreeNode(treeNode, AABB.Offset(smallBounds, new Vector3(halfSizeSplit.X, 0.0f, 0.0f)), hasChildLists);
            RenderTreeNode btl = new RenderTreeNode(treeNode, AABB.Offset(smallBounds, new Vector3(0.0f, 0.0f, halfSizeSplit.Z)), hasChildLists);
            RenderTreeNode btr = new RenderTreeNode(treeNode, AABB.Offset(smallBounds, new Vector3(halfSizeSplit.X, 0.0f, halfSizeSplit.Z)), hasChildLists);

            RenderTreeNode fbl = new RenderTreeNode(treeNode, AABB.Offset(smallBounds, new Vector3(0.0f, halfSizeSplit.Y, 0.0f)), hasChildLists);
            RenderTreeNode fbr = new RenderTreeNode(treeNode, AABB.Offset(smallBounds, new Vector3(halfSizeSplit.X, halfSizeSplit.Y, 0.0f)), hasChildLists);
            RenderTreeNode bbl = new RenderTreeNode(treeNode, AABB.Offset(smallBounds, new Vector3(0.0f, halfSizeSplit.Y, halfSizeSplit.Z)), hasChildLists);
            RenderTreeNode bbr = new RenderTreeNode(treeNode, AABB.Offset(smallBounds, halfSizeSplit), hasChildLists);

            return [
                ftl,
                ftr,
                btl,
                btr,

                fbl,
                fbr,
                bbl,
                bbr,
                ];
        }

        internal void AddEntityToTree(SceneEntity entity, TreeRegion nodeRegion)
        {
            if (!_childNodeIndices.TryGetValue(nodeRegion, out int indice))
            {
                throw new NotImplementedException("placeholder");
            }

            RenderTreeNodeData nodeData = _nodes[indice];
            nodeData.Node.AddEntity(entity);

            _containedEntityCount++;
        }

        internal void RemoveEntityFromTree(SceneEntity entity)
        {
            ref RenderTreeAdditionalInfo additionalInfo = ref entity.GetComponent<RenderTreeAdditionalInfo>();
            Debug.Assert(!Unsafe.IsNullRef(ref additionalInfo));

            if (!_childNodeIndices.TryGetValue(additionalInfo.NodeRegion, out int indice))
            {
                EngLog.Render.Warning("Cannot remove entity from tree as its owning node indice could not be found: {e} (tree: {t}, node: {n})", entity, additionalInfo.TreeRegion, additionalInfo.NodeRegion);
                return;
            }

            RenderTreeNodeData nodeData = _nodes[indice];
            if (!nodeData.Node.RemoveEntity(entity))
            {
                EngLog.Render.Warning("Unexpected error found while removing entity from tree node: {e} (tree: {t}, node: {n})", entity, additionalInfo.TreeRegion, additionalInfo.NodeRegion);
                _containedEntityCount--;
            }
        }

        internal void MoveEntityNode(SceneEntity entity, TreeRegion newNodeRegion)
        {
            ref RenderTreeAdditionalInfo additionalInfo = ref entity.GetComponent<RenderTreeAdditionalInfo>();

            Debug.Assert(!Unsafe.IsNullRef(ref additionalInfo));

            if (newNodeRegion == additionalInfo.NodeRegion)
            {
                EngLog.Render.Warning("Entity update: {e} is invalid as its bounding box center has not moved tree node.", RenderTreeManager.TreeUpdateType.MovedTree);
                return;
            }

            {
                if (_childNodeIndices.TryGetValue(additionalInfo.NodeRegion, out int indice))
                {
                    RenderTreeNodeData nodeData = _nodes[indice];
                    if (!nodeData.Node.RemoveEntity(entity))
                    {
                        EngLog.Render.Warning("Unexpected error found while removing entity from tree node: {e} (tree: {t}, node: {n})", entity, additionalInfo.TreeRegion, additionalInfo.NodeRegion);
                    }
                }
                else
                    EngLog.Render.Warning("Cannot remove entity from tree as its owning node indice could not be found: {e} (tree: {t}, node: {n})", entity, additionalInfo.TreeRegion, additionalInfo.NodeRegion);
            }

            {
                if (_childNodeIndices.TryGetValue(newNodeRegion, out int indice))
                {
                    RenderTreeNodeData nodeData = _nodes[indice];
                    nodeData.Node.AddEntity(entity);
                }
                else
                    EngLog.Render.Warning("Cannot remove entity from tree as its new owning node indice could not be found: {e} (tree: {t}, node: {n})", entity, additionalInfo.TreeRegion, newNodeRegion);
            }

            additionalInfo.NodeRegion = newNodeRegion;
        }

        public RenderTreeNodeData GetTreeNode(int indice) => _nodes[indice];

        public Vector3 BasePosition => _rootPosition;
        public RenderTreeNodeData RootNode => _nodes[0];

        public int ContainedEntityCount => _containedEntityCount;
    }

    public record struct RenderTreeNodeData(RenderTreeNode Node, int[] Subnodes, bool IsLeafNode);

    public enum RenderTreeNodeOctant : byte
    {
        FrontTopLeft = 0,
        FrontTopRight,
        BackTopLeft,
        BackTopRight,

        FrontBottomLeft,
        FrontBottomRight,
        BackBottomLeft,
        BackBottomRight
    }
}
