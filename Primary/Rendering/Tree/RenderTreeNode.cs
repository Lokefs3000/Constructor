using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Data;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering.Tree
{
    public sealed class RenderTreeNode
    {
        private readonly RenderTreeNode? _parent;
        private readonly AABB _boundaries;

        private List<TreeEntityData>? _childEntities;

        internal RenderTreeNode(RenderTreeNode? parent, AABB boundaries, bool hasChildList)
        {
            _parent = parent;
            _boundaries = boundaries;

            _childEntities = hasChildList ? new List<TreeEntityData>() : null;
        }

        internal void AddEntity(SceneEntity entity)
        {
            Debug.Assert(_childEntities != null);
            _childEntities?.Add(new TreeEntityData(entity));
        }

        internal bool RemoveEntity(SceneEntity entity)
        {
            Debug.Assert(_childEntities != null);
            return _childEntities?.Remove(new TreeEntityData(entity)) ?? false;
        }

        public RenderTreeNode? Parent => _parent;
        public AABB Boundaries => _boundaries;

        public IReadOnlyList<TreeEntityData>? Children => _childEntities;
    }

    public readonly record struct TreeEntityData(SceneEntity Entity);
}
