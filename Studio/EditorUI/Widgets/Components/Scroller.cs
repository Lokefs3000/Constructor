using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Common;
using EditorUI.Input;
using EditorUI.Styling;
using EditorUI.Visual;
using Primary.Common;
using Primary.Input.Devices;
using Primary.Mathematics;

namespace EditorUI.Widgets.Components
{
    public sealed class Scroller : StyledObject, IInteractable, IInteractionShape
    {
        private readonly ScrollView _scrollView;
        private readonly Scrollbars _direction;

        private bool _isEnabled;
        private bool _wasPreviouslyEnabled;

        private bool _isHovered;
        private bool _isHeld;
        private bool _isActive;

        private UIColor _scrollerColor;
        private Vector4 _cornerRadius;

        private ushort _strokeWidth;
        private StrokePosition _strokePosition;
        private UIColor _strokeColor;

        private float _computedSize;

        private float _dragScrollStart;

        private StateFlags _stateFlags;

        internal Scroller(ScrollView scrollView, Scrollbars direction)
        {
            _scrollView = scrollView;
            _direction = direction;

            _isEnabled = false;
            _wasPreviouslyEnabled = false;

            _isHovered = false;
            _isHeld = false;
            _isActive = false;

            _scrollerColor = Color.White;
            _cornerRadius = Vector4.Zero;

            _strokeWidth = 0;
            _strokePosition = StrokePosition.Inside;
            _strokeColor = Color.White;

            _computedSize = 0.0f;

            _dragScrollStart = -1.0f;

            _stateFlags = StateFlags.SelfInvalidStyle;
        }

        internal void Destroy()
        {
            UIManager.Instance.InputManager.ForgetInteractable(this);
        }

        internal void FinalizeLayout(float size)
        {
            _computedSize = size;
        }

        internal void PaintSelf(ref readonly PainterContext painter, Boundaries computedRect)
        {
            if (_direction == Scrollbars.Vertical)
            {
                float size = Math.Max(_scrollView.ContentSize.Y / _scrollView.ViewSize.Y, 0.2f);

                if (size < 1.0f)
                {
                    float maxAmountScrollable = 1.0f - size;
                    float scroll = Math.Clamp(_scrollView.ScrollPosition.Y / (_scrollView.ViewSize.Y - _scrollView.ContentSize.Y) * maxAmountScrollable, 0.0f, maxAmountScrollable);

                    if (!_isActive)
                    {
                        SetEditedField(true, nameof(_isActive));
                        _isActive = true;
                    }

                    float top = computedRect.Minimum.Y + scroll * _computedSize;
                    float bottom = top + size * _computedSize;

                    if (_scrollerColor.IsVisible)
                        painter.AddRectangle(new Boundaries(new Vector2(computedRect.Minimum.X, top), new Vector2(computedRect.Maximum.X, bottom)), new Paint(_scrollerColor, _strokeColor, _strokeWidth, _strokePosition), _cornerRadius);
                }
                else
                {
                    if (_isActive)
                    {
                        SetEditedField(false, nameof(_isActive));
                        _isActive = false;
                    }
                }
            }
            else
            {

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
                case UIInputEventType.MouseDown:
                    {
                        if (inputEvent.Mouse.Button == MouseButton.Left)
                        {
                            _isHeld = true;
                            SetEditedField(true, nameof(IsHeld));
                            return true;
                        }

                        break;
                    }
                case UIInputEventType.MouseUp:
                    {
                        if (inputEvent.Mouse.Button == MouseButton.Left)
                        {
                            _isHeld = false;
                            SetEditedField(false, nameof(IsHeld));
                            return true;
                        }

                        break;
                    }

                case UIInputEventType.DragBegin:
                    {
                        if (inputEvent.Drag.Button == MouseButton.Left)
                        {
                            _dragScrollStart = _direction == Scrollbars.Vertical ?
                                _scrollView.ScrollPosition.Y :
                                _scrollView.ScrollPosition.X;

                            return true;
                        }

                        break;
                    }
                case UIInputEventType.DragUpdate:
                case UIInputEventType.DragEnd:
                    {
                        if (inputEvent.Drag.Button == MouseButton.Left)
                        {
                            if (_direction == Scrollbars.Vertical)
                            {
                                float size = _scrollView.ContentSize.Y / _scrollView.ViewSize.Y;
                                if (size < 1.0f)
                                {
                                    float amountWithDelta = Math.Clamp(inputEvent.Drag.Delta.Y / size + _dragScrollStart, 0.0f, _scrollView.ViewSize.Y - _scrollView.ContentSize.Y);
                                    _scrollView.ScrollPosition = new Vector2(_scrollView.ScrollPosition.X, amountWithDelta);
                                }
                            }
                            else
                            {
                                float size = _scrollView.ContentSize.X / _scrollView.ViewSize.X;
                                if (size < 1.0f)
                                {
                                    float amountWithDelta = Math.Clamp(inputEvent.Drag.Delta.X / size + _dragScrollStart, 0.0f, _scrollView.ViewSize.X - _scrollView.ContentSize.X);
                                    _scrollView.ScrollPosition = new Vector2(amountWithDelta, _scrollView.ScrollPosition.Y);
                                }
                            }

                            return true;
                        }

                        break;
                    }
            }

            return false;
        }

        public override void AddStateFlags(StateFlags flags)
        {
            _stateFlags |= flags;
            if (_isEnabled)
                _scrollView.AddStateFlags(flags & ~StateFlags.This);
        }

        public override void RemoveStateFlags(StateFlags flags)
        {
            _stateFlags &= ~flags;
        }

        protected internal override void GetUnstyledObjects(ref StyleQueueContext queue)
        {
        }

        public IInteractable GetInteractable(Vector2 point) => this;

        public bool Intersects(Vector2 point)
        {
            return true;
        }

        protected internal override StyledObject? ParentObject => _scrollView;

        public IInteractionShape? Shape => this;
        public WidgetInputState InputState => WidgetInputState.Sink;

        public bool IsEnabled { get => _isEnabled; internal set { _wasPreviouslyEnabled = _isEnabled; _isEnabled = value; } }
        internal bool WasPreviouslyEnabled { get => _wasPreviouslyEnabled; set => _wasPreviouslyEnabled = value; }

        public float ComputedSize => _computedSize;

        public override StateFlags StateFlags => _stateFlags;

        #region Serializable
        [StyleTrigger, Styled(nameof(_isHovered), isEditable: true)]
        public bool IsHovered => _isHovered;
        [StyleTrigger, Styled(nameof(_isHeld), isEditable: true)]
        public bool IsHeld => _isHeld;
        [StyleTrigger, Styled(nameof(_isActive), isEditable: true)]
        public bool IsActive => _isActive;

        [Styled(nameof(_scrollerColor))] public UIColor ScrollerColor { get => _scrollerColor; set => SetStyledField(value); }
        [Styled(nameof(_cornerRadius))] public Vector4 CornerRadius { get => _cornerRadius; set => SetStyledField(value); }

        [Styled(nameof(_strokeColor))] public UIColor StrokeColor { get => _strokeColor; set => SetStyledField(value); }
        [Styled(nameof(_strokePosition))] public StrokePosition StrokePosition { get => _strokePosition; set => SetStyledField(value); }
        [Styled(nameof(_strokeWidth))] public ushort StrokeWidth { get => _strokeWidth; set => SetStyledField(value); }
        #endregion
    }
}
