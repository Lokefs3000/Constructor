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
using EditorUI.Widgets;
using Primary.Common;
using Primary.Mathematics;

namespace EditorUI.Popup.Components
{
    internal sealed class DropdownItem : StyledObject, IInteractable
    {
        private readonly DropdownMenuHost _host;

        private readonly string _value;
        private readonly int _index;

        private StateFlags _stateFlags;

        private bool _isHovered;
        private bool _isActive;

        private UIColor _backgroundColor;
        private UIColor _textColor;

        private TextShapingData? _shapingData;

        internal DropdownItem(DropdownMenuHost host, string value, int index)
        {
            _host = host;

            _value = value;
            _index = index;

            _stateFlags = StateFlags.SelfInvalidStyle;

            _backgroundColor = Color.TransparentWhite;
            _textColor = Color.White;

            _shapingData = null;

            if (_index == host.Index)
            {
                _isActive = true;
                SetEditedField(true, nameof(IsActive));
            }

            AddStateFlags(StateFlags.SelfInvalidStyle);
        }

        internal void DestroySelf()
        {
            UIManager.Instance.InputManager.ForgetInteractable(this);
            CleanupSelf();
        }

        internal void CleanupSelf()
        {
            if (_shapingData != null)
            {
                TextManager textManager = UIManager.Instance.TextManager;
                textManager.ForgetShapingDataFor(this);

                _shapingData = null;
            }
        }

        public void PaintSelf(ref readonly PainterContext painter, Boundaries boundaries)
        {
            if (_backgroundColor.IsVisible)
                painter.AddRectangle(boundaries, new Paint(_backgroundColor));

            if (_host.FontFamily != null && _host.FontFamily.IsReadyToUse && _textColor.IsVisible)
            {
                Boundaries innerBounds = Boundaries.Grow(boundaries, new Vector2(-4.0f, -2.0f));
                Vector2 innerSize = innerBounds.Size;

                if (_shapingData == null)
                {
                    TextManager textManager = UIManager.Instance.TextManager;
                    TextBuilder textBuilder = new TextBuilder(innerSize.X, TextWrapMode.Ellipsis, TextAlignment.CenterLeft, false);

                    textManager.GetOrShapeTextFor(this, ref _shapingData, _value, BuiltTextBuilder.Build(in textBuilder), _host.FontFamily.Value!.GetFontStyle(_host.FontStyle, _host.FontWeight), _host.FontSize);
                }

                painter.AddText(new Vector2(innerBounds.Minimum.X, innerBounds.Maximum.Y), _shapingData, innerSize, new Paint(_textColor));
            }
        }

        public bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            switch (inputEvent.EventType)
            {
                case UIInputEventType.MouseEnter:
                    {
                        _isHovered = true;
                        SetEditedField(true, nameof(IsHovered));
                        return true;
                    }
                case UIInputEventType.MouseLeave:
                    {
                        _isHovered = false;
                        SetEditedField(false, nameof(IsHovered));
                        return true;
                    }

                case UIInputEventType.MousePress:
                    {
                        _host.SelectNewIndex(_index);
                        return true;
                    }
            }

            return false;
        }

        public override void AddStateFlags(StateFlags flags)
        {
            _stateFlags |= flags;
            _host.AddStateFlags(flags & ~StateFlags.This);
        }

        public override void RemoveStateFlags(StateFlags flags)
        {
            _stateFlags &= ~flags;
        }

        protected internal override void GetUnstyledObjects(ref StyleQueueContext context)
        {
        }

        public IInteractable GetInteractable(Vector2 point) => this;

        public override StateFlags StateFlags => _stateFlags;
        protected internal override StyledObject? ParentObject => _host;

        public IInteractionShape? Shape => null;
        public WidgetInputState InputState => WidgetInputState.Sink;

        #region Serializable
        [StyleTrigger, Styled(nameof(_isHovered), isEditable: true)]
        public bool IsHovered => _isHovered;

        [StyleTrigger, Styled(nameof(_isActive), isEditable: true)]
        public bool IsActive => _isActive;

        [Styled(nameof(_backgroundColor))] public UIColor BackgroundColor { get => _backgroundColor; set => SetStyledField(value); }
        [Styled(nameof(_textColor))] public UIColor TextColor { get => _textColor; set => SetStyledField(value); }
        #endregion
    }
}
