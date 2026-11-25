using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Built;
using EditorUI.Common;
using EditorUI.Input;
using EditorUI.Styling;
using EditorUI.Text;
using EditorUI.Visual;
using Primary.Assets;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using TerraFX.Interop.Windows;

namespace EditorUI.Popup.Menu
{
    [TriggerValues("is-hovered", "is-held")]
    public sealed class PopupMenuMenu : PopupMenuItem
    {
        private readonly PopupMenu _owningMenu;
        private readonly PopupMenuMenu? _owningItem;

        private string _name;
        [StyleSetup(StateFlags.SelfInvalidLayout)] private string _text;

        private List<PopupMenuItem> _items;

        private Vector2 _itemMenuSize;
        private Vector2 _menuPosition;

        private TextShapingData? _shapingData;

        [StyleInclude] private UIColor _backgroundColor;
        [StyleInclude] private UIColor _textColor;
        [StyleSetup(StateFlags.SelfInvalidLayout)] private UIColor _arrowColor;

        [StyleInclude] private float _arrowThickness;

        private bool _isHovered;
        private bool _isHeld;

        internal PopupMenuMenu(PopupMenu owningMenu, PopupMenuMenu? owningItem, string name, string text)
        {
            _owningMenu = owningMenu;
            _owningItem = owningItem;

            _name = name;
            _text = text;

            _items = new List<PopupMenuItem>();

            _itemMenuSize = Vector2.NegativeZero;

            _backgroundColor = Color.TransparentBlack;
            _textColor = Color.Black;
            _arrowColor = Color.Black;

            _arrowThickness = 2.0f;
        }

        protected internal override void DestroySelf()
        {
            foreach (PopupMenuItem item in _items)
            {
                item.DestroySelf();
            }

            _items.Clear();
            base.DestroySelf();
        }

        protected internal override void MeasureSelf(float maxAvailableWidth)
        {
            if (_owningMenu.FontFamily == null || !_owningMenu.FontFamily.IsReadyToUse)
                return;

            if (_arrowColor.IsVisible)
                maxAvailableWidth -= _owningMenu.FontSize;

            if (_shapingData == null)
            {
                TextManager textManager = UIManager.Instance.TextManager;
                TextBuilder textBuilder = new TextBuilder(maxAvailableWidth);

                FontStyleData fontStyleData = _owningMenu.FontFamily.Value!.GetFontStyle(_owningMenu.FontStyle, _owningMenu.FontWeight);
                textManager.GetOrShapeTextFor(this, ref _shapingData, _text, BuiltTextBuilder.Build(in textBuilder), fontStyleData, _owningMenu.FontSize);
            }

            _itemWidth = _shapingData.TotalSize.X;
            _itemHeight = _shapingData.TotalSize.Y;

            if (_arrowColor.IsVisible)
                _itemWidth += _owningMenu.FontSize;
        }

        protected internal override void PaintSelf(ref readonly PainterContext painter, Vector2 originPosition, Vector4 safePadding, float availableWidth)
        {
            if (_shapingData != null)
            {
                if (_backgroundColor.IsVisible)
                    painter.AddRectangle(Boundaries.Grow(new Boundaries(originPosition, originPosition + new Vector2(availableWidth, _itemHeight)), safePadding), new Paint(_backgroundColor));

                if (_arrowColor.IsVisible)
                {
                    float fontSize = _owningMenu.FontSize;
                    availableWidth -= fontSize;

                    Span<Vector2> linePoints =
                    [
                        originPosition + new Vector2(availableWidth + 4.0f, 3.0f),
                        originPosition + new Vector2(availableWidth + fontSize - 4.0f, _itemHeight * 0.5f),
                        originPosition + new Vector2(availableWidth + 4.0f, _itemHeight - 3.0f),
                    ];

                    painter.AddLines(linePoints, new Paint(_arrowColor), LinePaintMode.Strip, _arrowThickness);
                }

                if (_textColor.IsVisible)
                    painter.AddText(new Vector2(originPosition.X, originPosition.Y + _itemHeight), _shapingData, new Vector2(availableWidth, _itemHeight), new Paint(_textColor));

            }
        }

        public override bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            switch (inputEvent.EventType)
            {
                case UIInputEventType.MouseEnter:
                    {
                        SetTriggerValue("is-hovered", true);
                        _owningMenu.BeginItemHover(this);
                        return true;
                    }
                case UIInputEventType.MouseLeave:
                    {
                        SetTriggerValue("is-hovered", false);
                        _owningMenu.EndItemHover(this);
                        return true;
                    }
                case UIInputEventType.MouseDown:
                    {
                        SetTriggerValue("is-held", true);
                        return true;
                    }
                case UIInputEventType.MouseUp:
                    {
                        SetTriggerValue("is-held", false);
                        return true;
                    }

                case UIInputEventType.MousePress:
                    {
                        _owningMenu.HandleItemPress(this);
                        return true;
                    }
            }

            return false;
        }

        protected internal override void ClearSavedData()
        {
            _itemMenuSize = Vector2.NegativeZero;

            if (_shapingData != null)
            {
                TextManager textManager = UIManager.Instance.TextManager;
                textManager.ForgetShapingDataFor(this);

                _shapingData = null;
            }

            foreach (PopupMenuItem menuItem in _items)
            {
                menuItem.ClearSavedData();
            }
        }

        protected internal override void ForgetState()
        {
            base.ForgetState();

            foreach (PopupMenuItem item in _items)
            {
                item.ForgetState();
            }

            _isHovered = false;
            _isHeld = false;

            SetTriggerValue("is-hovered", false);
            SetTriggerValue("is-held", false);
        }

        #region StyledObject
        public override void AddStateFlags(StateFlags flags)
        {
            _stateFlags |= StateFlags.SelfInvalidStyle;

            if (_owningItem != null)
                _owningItem.AddStateFlags(flags & ~StateFlags.This);
            else
                _owningMenu.AddStateFlags(flags & ~StateFlags.This);
        }

        public override void RemoveStateFlags(StateFlags flags)
        {
            _stateFlags &= ~flags;
        }

        protected internal override void GetUnstyledObjects(ref StyleQueueContext queue)
        {
            foreach (PopupMenuItem item in _items)
            {
                queue.TryEnqueue(item);
            }
        }

        protected internal override StyledObject? ParentObject => (StyledObject?)_owningItem ?? _owningMenu;
        #endregion

        public PopupMenuAction AddAction(string actionName, string actionText, Sprite? sprite)
        {
            _owningMenu.ThrowIfLocked();

            PopupMenuAction action = new PopupMenuAction(_owningMenu, this, actionName, actionText, sprite);
            _items.Add(action);

            return action;
        }

        public PopupMenuMenu AddMenu(string menuName, string menuText)
        {
            _owningMenu.ThrowIfLocked();

            PopupMenuMenu action = new PopupMenuMenu(_owningMenu, this, menuName, menuText);
            _items.Add(action);

            return action;
        }

        public PopupMenuSeparator AddSeparator()
        {
            _owningMenu.ThrowIfLocked();

            PopupMenuSeparator action = new PopupMenuSeparator(_owningMenu, this);
            _items.Add(action);

            return action;
        }

        #region Serializable
        public UIColor BackgroundColor { get => _backgroundColor; set => _backgroundColor = value; }
        public UIColor TextColor { get => _textColor; set => _textColor = value; }
        public UIColor ArrowColor { get => _arrowColor; set => _arrowColor = value; }

        public float ArrowThickness { get => _arrowThickness; set => _arrowThickness = value; }

        public bool IsHovered { get => _isHovered; }
        public bool IsHeld { get => _isHeld; }
        #endregion

        public override PopupMenu OwningMenu => _owningMenu;
        public override PopupMenuMenu? OwningItem => _owningItem;

        public string Name { get => _name; set => _name = value; }
        public string Text { get => _text; set => _text = value; }

        public ROList<PopupMenuItem> Items => _items;

        public Vector2 ItemMenuSize { get => _itemMenuSize; internal set => _itemMenuSize = value; }
        public Vector2 MenuPosition { get => _menuPosition; internal set => _menuPosition = value; }
    }
}
