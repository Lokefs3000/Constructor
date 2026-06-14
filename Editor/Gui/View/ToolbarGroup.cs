using Editor.Gui.View.Elements;
using Editor.UI.Elements;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Editor.Gui.View
{
    public sealed class ToolbarGroup
    {
        private readonly UIElement _rootElement;
        private readonly FrozenDictionary<string, ToolbarItem> _toolbarItems;

        internal ToolbarGroup(UIElement root, FrozenDictionary<string, ToolbarItem> items)
        {
            _rootElement = root;
            _toolbarItems = items;
        }

        public bool TryGetToolbarItem(string id, [NotNullWhen(true)] out ToolbarItem? item) => _toolbarItems.TryGetValue(id, out item);

        public UIElement RootElement => _rootElement;
    }
}
