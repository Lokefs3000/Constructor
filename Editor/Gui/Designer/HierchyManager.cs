using Editor.UI.Assets;
using Editor.UI.Designer;
using Editor.UI.Elements;
using Editor.UI.Elements.Tree;
using Primary.Assets;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.Designer
{
    internal sealed class HierchyManager
    {
        private readonly UIDesigner _designer;
        private readonly UITreeView _treeView;

        private readonly UIFontAsset _mainFont;

        private readonly ContextMenu _nodeContextMenu;

        private TreeNode _rootNode;

        internal HierchyManager(UIDesigner designer)
        {
            _designer = designer;
            _treeView = designer.FindElementWithId<UITreeView>("Hierchy") ?? throw new Exception("Failed to find hierchy tree view");

            _mainFont = AssetManager.LoadAsset<UIFontAsset>("Editor/Fonts/Inter.uifont")
                .WaitIfNotLoaded();

            _nodeContextMenu = AssetManager.LoadAsset<ContextMenu>("Editor/Designer/Context/HierchyContext.uimenu");

            _rootNode = new TreeNode()
            {
                Parent = _treeView.RootNode,

                FontStyle = _mainFont.FindStyle("Regular"),
                Text = "Root element",

                TextColor = s_treeViewColor
            };
        }

        internal void ClearView()
        {
            _rootNode.ClearChildren();
        }

        private static readonly Color s_treeViewColor = Color.FromHex("6da4fc");
    }
}
