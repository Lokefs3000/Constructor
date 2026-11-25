using Editor.UI;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Interaction;
using Editor.UI.Popup;
using Editor.UI.Serialization;
using Editor.UI.Visual;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Gui.View.Elements
{
    public sealed class ToolbarDropdownItem : ToolbarItem
    {
        private LayoutSnippet? _dropdownSnippet;

        public ToolbarDropdownItem()
        {
            _dropdownSnippet = null;

            _isExpandable = true;
        }

        public ToolbarDropdownItem(UIElement parent) : this()
        {
            SetParent(parent);
        }

        protected override void Expand()
        {
            if (_dropdownSnippet != null && _dropdownSnippet.RootElement != null)
            {
                if (_dropdownSnippet.RootElement.Parent == this)
                {
                    _dropdownSnippet.RootElement.ClearParent();
                    OnSnippetCollapsed?.Invoke(_dropdownSnippet);
                }
                else
                {
                    _dropdownSnippet.RootElement.Parent = this;
                    _dropdownSnippet.RootElement.Position = new UIValue2(0.0f, 1.0f);
                    OnSnippetExpanded?.Invoke(_dropdownSnippet);
                }
            }
           
            base.Expand();
        }

        public LayoutSnippet DropdownSnippet { get => _dropdownSnippet!; internal set => _dropdownSnippet = value; }

        #region Events
        public event Action<LayoutSnippet>? OnSnippetExpanded;
        public event Action<LayoutSnippet>? OnSnippetCollapsed;
        #endregion
    }
}
