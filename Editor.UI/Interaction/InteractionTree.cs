using Editor.UI.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Interaction
{
    internal sealed class InteractionTree
    {
        private List<UIElement> _elements;
        private Dictionary<UIElement, ElementTreeNode> _treeNodes;

        internal InteractionTree()
        {
            _elements = new List<UIElement>();
            _treeNodes = new Dictionary<UIElement, ElementTreeNode>();
        }

        internal void AddElement(UIElement element)
        {
            if (_elements.Count == 0)
            {
                _elements.Add(element);
                _treeNodes.Add(element, new ElementTreeNode(new HashSet<UIElement>([element])));
                return;
            }

            UIElement? parent = element;
            do
            {

            } while ((parent = parent.Parent) != null);
        }

        private readonly record struct ElementTreeNode(HashSet<UIElement> Connections);
    }
}
