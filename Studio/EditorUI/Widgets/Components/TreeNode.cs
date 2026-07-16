using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Assets;
using EditorUI.Built;
using EditorUI.Common;
using EditorUI.Input;
using EditorUI.Text;
using EditorUI.Visual;
using EditorUI.Widgets.Stylists;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using TerraFX.Interop.Gdiplus;

namespace EditorUI.Widgets.Components
{
    public class TreeNode : BaseTreeNode
    {
        protected TreeNodeStylist? _stylist;

        protected string? _id;
        protected string? _hoveredId;
        protected string? _selectedId;

        protected bool _isHovered;

        protected string? _text;

        public TreeNode()
        {
            _stylist = null;

            _id = null;
            _hoveredId = "is-hovered";
            _selectedId = "is-selected";

            _text = null;
        }

        protected override void OnParentTreeChanged(TreeView? newTreeView)
        {
            _stylist = newTreeView?.FindStylist<TreeNodeStylist>(_id);

            if (ParentTree != null)
            {
                ParentTree.OnStylistDestroyed -= OnStylistDestroyed;
                ParentTree.OnStylistIdChanged -= OnStylistIdChanged;
            }

            if (newTreeView != null)
            {
                newTreeView.OnStylistDestroyed += OnStylistDestroyed;
                newTreeView.OnStylistIdChanged += OnStylistIdChanged;
            }
        }

        protected override void OnSelected()
        {
            TryUpdateCurrentStylist();
        }

        protected override void OnDeselected()
        {
            TryUpdateCurrentStylist();
        }

        protected internal override void PaintSelf(ref readonly PainterContext painter, Vector2 availableSize)
        {
            if (_stylist != null)
            {
                if (_stylist.BackgroundColor.IsVisible)
                    painter.AddRectangle(new Boundaries(Vector2.Zero, availableSize), new Paint(_stylist.BackgroundColor));
                if (!string.IsNullOrEmpty(_text) && _stylist.FontFamily != null && _stylist.TextColor.IsVisible && _stylist.FontFamily.IsReadyToUse && _stylist.FontFamily.Value != null)
                {
                    TextManager textManager = UIManager.Instance.TextManager;
                    TextBuilder textBuilder = new TextBuilder(availableSize.X, AllowRichText: _stylist.AllowRichText);
                    TextShapingData shapingData = textManager.ShapeText(_text, _stylist.FontSize, BuiltTextBuilder.Build(in textBuilder), _stylist.FontFamily.Value.GetFontStyle(_stylist.FontStyle, _stylist.FontWeight));

                    painter.AddText(new Vector2(0.0f, availableSize.Y - (shapingData.TotalSize.Y - _stylist.FontSize * 0.75f)), shapingData, availableSize, new Paint(_stylist.TextColor));
                }
            }
        }

        public override bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            switch (inputEvent.EventType)
            {
                case UIInputEventType.MouseEnter:
                    {
                        _isHovered = true;
                        TryUpdateCurrentStylist();
                        return true;
                    }
                case UIInputEventType.MouseLeave:
                    {
                        _isHovered = false;
                        TryUpdateCurrentStylist();
                        return true;
                    }

                case UIInputEventType.MouseDown:
                    {
                        if (inputEvent.Mouse.Button == MouseButton.Left)
                        {
                            if (!InputSystem.Keyboard.KeyModifiers.HasAny(KeyModifier.Control))
                                ParentTree?.ClearSelectedNodes();
                            Select();

                            return true;
                        }
                        break;
                    }
            }

            return false;
        }

        private void TryUpdateCurrentStylist()
        {
            string? currentStyleId = null;
            if (IsSelected && currentStyleId == null)
                currentStyleId = _selectedId;
            if (_isHovered && currentStyleId == null)
                currentStyleId = _hoveredId;

            currentStyleId ??= _id;

            if (_stylist == null || _stylist.Id != currentStyleId)
            {
                _stylist = ParentTree?.FindStylist<TreeNodeStylist>(currentStyleId);
            }
        }

        private void OnStylistDestroyed(Widget widget, Stylist stylist)
        {
            if (stylist == _stylist)
            {
                TryUpdateCurrentStylist();
            }
        }

        private void OnStylistIdChanged(Widget widget, Stylist stylist)
        {
            if (stylist == _stylist && stylist.Id != _id)
            {
                TryUpdateCurrentStylist();
            }
        }

        public TreeNodeStylist? Stylist => _stylist;

        public string? StylistId
        {
            get => _id;
            set
            {
                if (value != _id)
                {
                    _id = value;
                    _stylist = ParentTree?.FindStylist<TreeNodeStylist>(_id);
                }
            }
        }

        public string? Text { get => _text; set => _text = value; }
    }
}
