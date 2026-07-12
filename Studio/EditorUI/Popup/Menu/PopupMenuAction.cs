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
using Primary.Common;
using Primary.Mathematics;
using TerraFX.Interop.Windows;

namespace EditorUI.Popup.Menu
{
    public sealed class PopupMenuAction : PopupMenuItem
    {
        private readonly PopupMenu _owningMenu;
        private readonly PopupMenuMenu? _owningItem;

        private string _name;
        private string _text;
        private Sprite? _sprite;

        private TextShapingData? _shapingData;

        private UIColor _backgroundColor;
        private UIColor _textColor;

        private bool _isHovered;
        private bool _isHeld;

        internal PopupMenuAction(PopupMenu owningMenu, PopupMenuMenu? owningItem, string name, string text, Sprite? sprite)
        {
            _owningMenu = owningMenu;
            _owningItem = owningItem;

            _name = name;
            _text = text;
            _sprite = sprite;

            _shapingData = null;

            _backgroundColor = Color.TransparentBlack;
            _textColor = Color.Black;

            _isHovered = false;
            _isHeld = false;
        }

        protected internal override void MeasureSelf(float maxAvailableWidth)
        {
            if (_owningMenu.FontFamily == null || !_owningMenu.FontFamily.IsReadyToUse)
                return;

            float requiredAdditional = _sprite != null ? _owningMenu.FontSize + 4.0f : 0.0f;

            if (_shapingData == null)
            {
                TextManager textManager = UIManager.Instance.TextManager;
                TextBuilder textBuilder = new TextBuilder(maxAvailableWidth - requiredAdditional);

                FontStyleData fontStyleData = _owningMenu.FontFamily.Value!.GetFontStyle(_owningMenu.FontStyle, _owningMenu.FontWeight);
                textManager.GetOrShapeTextFor(this, ref _shapingData, _text, BuiltTextBuilder.Build(in textBuilder), fontStyleData, _owningMenu.FontSize);
            }

            _itemWidth = _shapingData.TotalSize.X + requiredAdditional;
            _itemHeight = _shapingData.TotalSize.Y;
        }

        protected internal override void PaintSelf(ref readonly PainterContext painter, Vector2 originPosition, Vector4 safePadding, float availableWidth)
        {
            if (_shapingData != null)
            {
                float requiredAdditional = _sprite != null ? _owningMenu.FontSize + 4.0f : 0.0f;

                if (_backgroundColor.IsVisible)
                    painter.AddRectangle(Boundaries.Grow(new Boundaries(originPosition, originPosition + new Vector2(availableWidth, _itemHeight)), safePadding), new Paint(_backgroundColor));
                if (_sprite != null)
                    painter.AddImage(new Boundaries(originPosition, originPosition + new Vector2(_itemHeight)), _sprite, new Paint(Color.White));
                if (_textColor.IsVisible)
                    painter.AddText(new Vector2(originPosition.X + requiredAdditional, originPosition.Y + _itemHeight), _shapingData, new Vector2(_itemWidth - requiredAdditional, _itemHeight), new Paint(_textColor));
            }
        }

        public override void HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            switch (inputEvent.EventType)
            {
                case UIInputEventType.MouseEnter:
                    {
                        _isHovered = true;
                        SetEditedField(true, nameof(IsHovered));

                        _owningMenu.BeginItemHover(this);
                        break;
                    }
                case UIInputEventType.MouseLeave:
                    {
                        _isHovered = false;
                        SetEditedField(false, nameof(IsHovered));

                        _owningMenu.EndItemHover(this);
                        break;
                    }
                case UIInputEventType.MouseDown:
                    {
                        _isHeld = true;
                        SetEditedField(true, nameof(IsHeld));
                        break;
                    }
                case UIInputEventType.MouseUp:
                    {
                        _isHeld = false;
                        SetEditedField(false, nameof(IsHeld));
                        break;
                    }

                case UIInputEventType.MousePress:
                    {
                        _owningMenu.HandleItemPress(this);
                        break;
                    }
            }
        }

        protected internal override void ClearSavedData()
        {
            if (_shapingData != null)
            {
                TextManager textManager = UIManager.Instance.TextManager;
                textManager.ForgetShapingDataFor(this);

                _shapingData = null;
            }
        }

        protected internal override void ForgetState()
        {
            base.ForgetState();

            _isHovered = false;
            _isHeld = false;

            SetEditedField(false, nameof(IsHovered));
            SetEditedField(false, nameof(IsHeld));
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
        }

        protected internal override StyledObject? ParentObject => (StyledObject?)_owningItem ?? _owningMenu;
        #endregion

        #region Serializable
        [Styled(nameof(_backgroundColor))] public UIColor BackgroundColor { get => _backgroundColor; set => _backgroundColor = value; }
        [Styled(nameof(_textColor))] public UIColor TextColor { get => _textColor; set => _textColor = value; }

        [StyleTrigger, Styled(nameof(_isHovered), isEditable: true)]
        public bool IsHovered { get => _isHovered; }

        [StyleTrigger, Styled(nameof(_isHeld), isEditable: true)]
        public bool IsHeld { get => _isHeld; }
        #endregion

        public override PopupMenu OwningMenu => _owningMenu;
        public override PopupMenuMenu? OwningItem => _owningItem;

        public string Name { get => _name; set => _name = value; }
        public string Text { get => _text; set => _text = value; }
    }
}
