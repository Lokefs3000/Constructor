using Editor.UI;
using Editor.UI.Elements;
using Editor.UI.Visual;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.View.Elements
{
    public sealed class ToolbarEnumItem : ToolbarItem
    {
        private Type? _enumType;

        public ToolbarEnumItem()
        {
            _enumType = null;

            _isExpandable = true;
        }

        public ToolbarEnumItem(UIElement parent) : this()
        {
            SetParent(parent);
        }

        public Type EnumType { get => _enumType!; internal set => _enumType = value; }
    }
}
