using Editor.UI.Datatypes;
using Editor.UI.Elements.Composite;
using Editor.UI.Interaction;
using Editor.UI.Layout;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("ScrollView")]
    public class UIScrollView : UIFrame
    {
        private ScrollViewMode _mode;

        private ScrollbarVisibility _verticalVisiblity;
        private ScrollbarVisibility _horizontalVisibility;

        private Scroller _verticalScroller;
        private Scroller _horizontalScroller;

        private ScrollViewMode _currentViewMode;

        private UIColor _scrollerBackgroundColor;

        public UIScrollView()
        {
            _mode = ScrollViewMode.Vertical;

            _verticalVisiblity = ScrollbarVisibility.Auto;
            _horizontalVisibility = ScrollbarVisibility.Auto;

            _verticalScroller = new Scroller(this) { Direction = ScrollerDirection.Vertical };
            _horizontalScroller = new Scroller(this) { Direction = ScrollerDirection.Horizontal };

            _currentViewMode = ScrollViewMode.None;

            _scrollerBackgroundColor = new Color(0.5f);

            _verticalScroller.OnScroll += VerticalCallback;
            _horizontalScroller.OnScroll += HorizontalCallback;

            _backgroundColor = Color.TransparentWhite;
        }

        public UIScrollView(UIElement parent) : this()
        {
            SetParent(parent);
        }

        protected override void DestroySelf()
        {
            _verticalScroller.OnScroll -= VerticalCallback;
            _horizontalScroller.OnScroll -= HorizontalCallback;

            _verticalScroller.Destroy();
            _horizontalScroller.Destroy();
        }

        protected override void WindowChangingSelf(IElementOwner? previousWindow, bool isCascaded)
        {
            if (previousWindow != null)
            {
                previousWindow.StyleUpdater.RemoveInvalidStyleBase(_verticalScroller);
                previousWindow.StyleUpdater.RemoveInvalidStyleBase(_horizontalScroller);
            }

            _verticalScroller.InvalidateAll(_verticalScroller.CurrentStateName);
            _horizontalScroller.InvalidateAll(_horizontalScroller.CurrentStateName);
        }

        protected override void ScrollPositionChanged()
        {
            _verticalScroller.Value = _scrollPosition.X;
            _horizontalScroller.Value = _scrollPosition.Y;
        }

        public override void MeasureSize(UIMeasureContext context)
        {
            base.MeasureSize(context);

            if (Flags.HasFlag(_currentViewMode, ScrollViewMode.Vertical))
                _viewSize = new Vector2(-_verticalScroller.Width, 0.0f);
            if (Flags.HasFlag(_currentViewMode, ScrollViewMode.Horizontal))
                _viewSize = new Vector2(0.0f, -_horizontalScroller.Width);
        }

        public override void FinalizeLayout()
        {
            ScrollViewMode prevMode = _currentViewMode;
            _currentViewMode = ScrollViewMode.None;

            if (Flags.HasFlag(_mode, ScrollViewMode.Vertical))
            {
                if (_verticalVisiblity == ScrollbarVisibility.AlwaysVisible || (_verticalVisiblity == ScrollbarVisibility.Auto && _childExtents.Y > _viewSize.Y))
                {
                    _currentViewMode |= ScrollViewMode.Vertical;
                }
            }

            if (Flags.HasFlag(_mode, ScrollViewMode.Horizontal))
            {
                if (_horizontalVisibility == ScrollbarVisibility.AlwaysVisible || (_horizontalVisibility == ScrollbarVisibility.Auto && _childExtents.X > _viewSize.X))
                {
                    _currentViewMode |= ScrollViewMode.Horizontal;
                }
            }

            switch (_currentViewMode)
            {
                case ScrollViewMode.Vertical:
                    {
                        if (Flags.HasFlag(prevMode, ScrollViewMode.Horizontal))
                            WindowOwner?.ParentHost?.InteractionManager?.DereferenceDestroyedInteractable(_horizontalScroller);

                        _verticalScroller.FinalizeLayout();
                        break;
                    }
                case ScrollViewMode.Horizontal:
                    {
                        if (Flags.HasFlag(prevMode, ScrollViewMode.Vertical))
                            WindowOwner?.ParentHost?.InteractionManager?.DereferenceDestroyedInteractable(_verticalScroller);

                        _horizontalScroller.FinalizeLayout();
                        break;
                    }
                case ScrollViewMode.VerticalAndHorizontal:
                    {
                        _verticalScroller.FinalizeLayout(true);
                        _horizontalScroller.FinalizeLayout(true);
                        break;
                    }
            }
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            base.DrawVisual(painter);

            if (Flags.HasFlag(_currentViewMode, ScrollViewMode.Vertical))
                _verticalScroller.DrawVisual(painter);
            if (Flags.HasFlag(_currentViewMode, ScrollViewMode.Horizontal))
                _horizontalScroller.DrawVisual(painter);
            if (_currentViewMode == ScrollViewMode.VerticalAndHorizontal)
                painter.DrawRect(new Boundaries(new Vector2(_horizontalScroller.ScrollerBoundaries.Maximum.X, _verticalScroller.ScrollerBoundaries.Maximum.Y), _pixelCoordinates.Maximum), UIPaint.FromColor(_scrollerBackgroundColor));

            return true;
        }

        public override void HandleEvent(ref readonly UIEvent @event)
        {
            switch (@event.Type)
            {
                case UIEventType.MouseWheel:
                    {
                        if (InputSystem.Keyboard.IsKeyDown(KeyCode.LeftShift))
                        {
                            if (Flags.HasFlag(_currentViewMode, ScrollViewMode.Horizontal) && _viewSize.X < _childExtents.X)
                            {
                                _horizontalScroller.Scroll = Math.Clamp(_horizontalScroller.Scroll - @event.Mouse.Delta.Y * 40.0f, 0.0f, _childExtents.X - _viewSize.X);
                                break;
                            }
                        }
                        else
                        {
                            if (Flags.HasFlag(_currentViewMode, ScrollViewMode.Vertical) && _viewSize.Y < _childExtents.Y)
                            {
                                _verticalScroller.Scroll = Math.Clamp(_verticalScroller.Scroll - @event.Mouse.Delta.Y * 40.0f, 0.0f, _childExtents.Y - _viewSize.Y);
                                break;
                            }
                        }

                        goto default;
                    }
                default: base.HandleEvent(in @event); break;
            }
        }

        private void VerticalCallback(float value) => ScrollPosition = new Vector2(ScrollPosition.X, value);
        private void HorizontalCallback(float value) => ScrollPosition = new Vector2(value, ScrollPosition.Y);

        public override IInteractable GetInteractable(Vector2 point)
        {
            switch (_currentViewMode)
            {
                case ScrollViewMode.Vertical:
                    {
                        if (point.X >= _verticalScroller.ScrollerBoundaries.Minimum.X)
                            return _verticalScroller.GetInteractable(point);
                        break;
                    }
                case ScrollViewMode.Horizontal:
                    {
                        if (point.Y >= _horizontalScroller.ScrollerBoundaries.Minimum.Y)
                            return _horizontalScroller.GetInteractable(point);
                        break;
                    }
                case ScrollViewMode.VerticalAndHorizontal:
                    {
                        if (point.X >= _verticalScroller.ScrollerBoundaries.Minimum.X && point.Y <= _verticalScroller.ScrollerBoundaries.Maximum.Y)
                            return _verticalScroller.GetInteractable(point);
                        if (point.Y >= _horizontalScroller.ScrollerBoundaries.Minimum.Y && point.X <= _horizontalScroller.ScrollerBoundaries.Maximum.X)
                            return _horizontalScroller.GetInteractable(point);
                        break;
                    }
            }

            return base.GetInteractable(point);
        }

        public Scroller Vertical => _verticalScroller;
        public Scroller Horizontal => _horizontalScroller;

        #region Properties
        [EditableProperty(nameof(_mode), UIStateFlags.InvalidAll)] public ScrollViewMode Mode { get => _mode; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_verticalVisiblity), UIStateFlags.InvalidAll)] public ScrollbarVisibility VerticalVisiblity { get => _verticalVisiblity; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_horizontalVisibility), UIStateFlags.InvalidAll)] public ScrollbarVisibility HorizontalVisiblity { get => _horizontalVisibility; set => SetEditableProperty(value); }

        [StyleableProperty(nameof(_scrollerBackgroundColor), UIStateFlags.InvalidVisual)] public UIColor ScrollerBackgroundColor { get => _scrollerBackgroundColor; set => SetStyleProperty(value); }
        #endregion
    }

    public enum ScrollViewMode : byte
    {
        None = 0,

        Vertical = 1 << 0,
        Horizontal = 1 << 1,

        VerticalAndHorizontal = Vertical | Horizontal
    }

    public enum ScrollbarVisibility : byte
    {
        Auto = 0,
        AlwaysVisible,
        Hidden
    }
}
