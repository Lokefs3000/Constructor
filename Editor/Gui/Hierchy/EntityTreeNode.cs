using Editor.UI.Elements.Tree;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.Hierchy
{
    public class EntityTreeNode : TreeNode
    {
        protected readonly SceneEntity _entity;

        public EntityTreeNode(SceneEntity entity)
        {
            _entity = entity;
        }

        public SceneEntity Entity => _entity;
    }
}
