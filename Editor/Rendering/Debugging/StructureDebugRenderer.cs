using Primary;
using Primary.Common;
using Primary.Components;
using Primary.Rendering;
using Primary.Rendering.Tree;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Rendering.Debugging
{
    internal sealed class StructureDebugRenderer
    {
        internal void Render()
        {
            if (RenderDebug.DrawRenderTree)
            {
                RenderingManager rendering = Editor.GlobalSingleton.RenderingManager;
                RenderTreeManager treeManager = rendering.RenderTreeManager;

                foreach (var kvp in treeManager.Trees)
                {
                    RenderTree tree = kvp.Value;
                    DrawTreeNodesRecursie(tree, tree.RootNode, 0);
                }
            }
        }

        private void DrawTreeNodesRecursie(RenderTree tree, RenderTreeNodeData nodeData, int depth)
        {
            AABB aabb = nodeData.Node.Boundaries;
            int newDepth = depth + 1;

            foreach (int indice in nodeData.Subnodes)
            {
                RenderTreeNodeData subNodeData = tree.GetTreeNode(indice);
                DrawTreeNodesRecursie(tree, subNodeData, newDepth);
            }

            if (nodeData.Node.Children != null)
            {
                Vector3 center = tree.BasePosition + nodeData.Node.Boundaries.Center;
                if (RenderDebug.DrawEntityTreeConnections)
                {
                    foreach (TreeEntityData entity in nodeData.Node.Children)
                    {
                        ref WorldTransform wt = ref entity.Entity.GetComponent<WorldTransform>();
                        Gizmos.DrawLine(center, wt.Transformation.Translation, new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
                    }
                }

                Gizmos.DrawVector(center);
            }

            Gizmos.DrawWireCube(AABB.Offset(aabb, tree.BasePosition), new Vector4(0.5f + (depth / (float)Constants.rRenderTreeDepth), 0.0f, 0.0f, 1.0f));
        }
    }
}
