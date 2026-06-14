using Editor.UI;
using Editor.UI.Assets;
using Editor.UI.Designer;
using Editor.UI.Elements;
using Editor.UI.Elements.Tree;
using Editor.UI.Reflection;
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

        private readonly ContextMenuAsset _nodeContextMenu;

        private TreeNode _rootNode;
        private Dictionary<UIElement, TreeNode> _nodeDict;

        internal HierchyManager(UIDesigner designer)
        {
            _designer = designer;
            _treeView = designer.FindElementWithId<UITreeView>("Hierchy") ?? throw new Exception("Failed to find hierchy tree view");

            _mainFont = AssetManager.LoadAsset<UIFontAsset>("Editor/Fonts/Inter.uifont")
                .WaitIfNotLoaded();

            _nodeContextMenu = AssetManager.LoadAsset<ContextMenuAsset>("Editor/Designer/Context/HierchyContext.uimenu");

            _rootNode = new TreeNode()
            {
                Text = "Root element",

                TextColor = s_treeViewColor
            };
            _nodeDict = new Dictionary<UIElement, TreeNode>();

            _treeView.AddNode(_rootNode);
        }

        internal void ClearView()
        {
            _rootNode.ClearChildren();
            _nodeDict.Clear();

            _nodeDict.Add(_designer.CanvasManager.RootElement, _rootNode);
        }

        internal TreeNode? AddHierchyElement(UIElement element)
        {
            UIElement? parent = element.Parent;
            if (parent == null)
                return null;

            if (!_nodeDict.TryGetValue(parent, out TreeNode? parentNode))
            {
                EdLog.Gui.Error("No node created for parent: {p}", parent);

                parentNode = AddHierchyElement(parent);
                if (parentNode == null)
                    return null;
            }

            CachedElementData elementData = UIManager.Instance.ReflectionManager.ElementCache.GetElementData(element.GetType());

            TreeNode newNode = new TreeNode()
            {
                Font = _mainFont,
                Text = elementData.PrettyName,

                TextColor = s_treeViewColor
            };

            parentNode.AddNode(newNode);

            _nodeDict.Add(element, newNode);
            return newNode;
        }

        private static readonly Color s_treeViewColor = Color.FromHex("6da4fc");
    }
}
