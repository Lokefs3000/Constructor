using Editor.Gui.Windows;
using Editor.UI.Elements.Tree;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.Hierchy
{
    public abstract class HierchyNodeView
    {
        protected readonly HierchyWindow _hierchy;

        public HierchyNodeView(HierchyWindow window)
        {
            _hierchy = window;
        }

        public abstract EntityTreeNode CreateTreeNode(SceneEntity entity);
        public abstract Type GetNodeTypeFor(SceneEntity entity);
    }

    public class HierchyNodeViewComponents(params Type[] types) : Attribute
    {
        public Type[] Types { init; get; } = types;
    }
}
