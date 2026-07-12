using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Common;
using EditorUI.Input;
using EditorUI.Layout;
using EditorUI.Styling;
using EditorUI.Visual;
using EditorUI.Widgets.Components;
using Primary.Common;
using Primary.Mathematics;

namespace EditorUI.Widgets
{
    [UIWidget]
    public class ScrollView : Widget
    {
        protected Scrollbars _scrollbars;

        protected ScrollbarVisibility _verticalScrollbar;
        protected ScrollbarVisibility _horizontalScrollbar;

        protected float _scrollbarWidth;

        protected UIColor _scrollbarBackgroundColor;

        protected Scroller _verticalScroller;
        protected Scroller _horizontalScroller;

        protected Vector2 _insetIdealSize;
        protected Vector2 _scrollPosition;

        public ScrollView()
        {
            _scrollbars = Scrollbars.Both;

            _verticalScrollbar = ScrollbarVisibility.Auto;
            _horizontalScrollbar = ScrollbarVisibility.Auto;

            _scrollbarWidth = 6.0f;

            _scrollbarBackgroundColor = Color.White;

            _verticalScroller = new Scroller(this, Scrollbars.Vertical);
            _horizontalScroller = new Scroller(this, Scrollbars.Horizontal);

            _insetIdealSize = Vector2.Zero;
            _scrollPosition = Vector2.Zero;
        }

        protected internal override void DestroySelf()
        {
            _verticalScroller.Destroy();
            _horizontalScroller.Destroy();
            base.DestroySelf();
        }

        protected internal override MeasureReturnData MeasureSelf(ref readonly LayoutContext context)
        {
            MeasureReturnData returnData = base.MeasureSelf(in context);
            if (returnData.Status == MeasureStatus.Success)
            {
                _insetIdealSize = _idealSize;

                if (Flags.HasFlag(_scrollbars, Scrollbars.Vertical) && _verticalScrollbar == ScrollbarVisibility.Always)
                    _insetIdealSize.X -= _scrollbarWidth;
                if (Flags.HasFlag(_scrollbars, Scrollbars.Horizontal) && _horizontalScrollbar == ScrollbarVisibility.Always)
                    _insetIdealSize.Y -= _scrollbarWidth;

                returnData = new MeasureReturnData(returnData.Status, returnData.LockAxis, _insetIdealSize);
            }

            return returnData;
        }

        protected internal override void FinalizeSelf(ref readonly LayoutContext context)
        {
            Vector2 maxChildExtents = _viewSize;
            if (_children != null)
            {
                foreach (Widget child in _children)
                {
                    maxChildExtents = Vector2.Max(child.IdealPosition + child.IdealSize, maxChildExtents);
                }

                _viewSize = Vector2.Max(maxChildExtents, _viewSize);
            }

            _verticalScroller.IsEnabled = Flags.HasFlag(_scrollbars, Scrollbars.Vertical) && (_verticalScrollbar == ScrollbarVisibility.Always || (_verticalScrollbar == ScrollbarVisibility.Auto && maxChildExtents.Y > _idealSize.Y));
            _horizontalScroller.IsEnabled = Flags.HasFlag(_scrollbars, Scrollbars.Horizontal) && (_horizontalScrollbar == ScrollbarVisibility.Always || (_horizontalScrollbar == ScrollbarVisibility.Auto && maxChildExtents.X > _idealSize.X));

            _insetIdealSize = _idealSize;

            if (_verticalScroller.IsEnabled)
            {
                _insetIdealSize.X -= _scrollbarWidth;
                _verticalScroller.FinalizeLayout(_insetIdealSize.Y);
            }
            else if (_verticalScroller.WasPreviouslyEnabled)
            {
                UIManager.Instance.InputManager.ForgetInteractable(_verticalScroller);
                _verticalScroller.WasPreviouslyEnabled = false;
            }

            if (_horizontalScroller.IsEnabled)
            {
                _insetIdealSize.Y -= _scrollbarWidth;
                _horizontalScroller.FinalizeLayout(_insetIdealSize.X);
            }
            else if (_horizontalScroller.WasPreviouslyEnabled)
            {
                UIManager.Instance.InputManager.ForgetInteractable(_horizontalScroller);
                _horizontalScroller.WasPreviouslyEnabled = false;
            }

            _scrollPosition = Vector2.Clamp(_scrollPosition, Vector2.Zero, Vector2.Max(Vector2.Zero, _viewSize - _insetIdealSize));
            _viewSize = Vector2.Max(maxChildExtents, _insetIdealSize);
        }

        protected internal override void PaintSelf(ref PainterContext painter)
        {
            base.PaintSelf(ref painter);

            if (_verticalScroller.IsEnabled)
            {
                Boundaries boundaries = new Boundaries(
                   new Vector2(_computedRect.Maximum.X - _scrollbarWidth, _computedRect.Minimum.Y),
                   new Vector2(_computedRect.Maximum.X, _computedRect.Minimum.Y + _insetIdealSize.Y));

                if (_scrollbarBackgroundColor.IsVisible)
                    painter.AddRectangle(boundaries, new Paint(_scrollbarBackgroundColor));

                _verticalScroller.PaintSelf(in painter, boundaries);
            }

            if (_horizontalScroller.IsEnabled)
            {
                Boundaries boundaries = new Boundaries(
                 new Vector2(_computedRect.Minimum.X, _computedRect.Maximum.Y - _scrollbarWidth),
                 new Vector2(_computedRect.Minimum.X + _insetIdealSize.X, _computedRect.Maximum.Y));

                if (_scrollbarBackgroundColor.IsVisible)
                    painter.AddRectangle(boundaries, new Paint(_scrollbarBackgroundColor));

                _horizontalScroller.PaintSelf(in painter, boundaries);
            }

            painter.PushClippingRect(new Rect(_computedRect.Minimum.AsInt2(), _insetIdealSize.AsInt2()));
            painter.PushTranslate(-_scrollPosition);
        }

        public override IInteractable GetInteractable(Vector2 point)
        {
            Vector2 localPoint = point - _computedRect.Minimum + Vector2.One;

            bool isAnyOver = Vector2.GreaterThanOrEqualAny(localPoint, _insetIdealSize);
            if (isAnyOver)
            {
                if (_verticalScroller.IsEnabled && localPoint.X >= _insetIdealSize.X)
                    return _verticalScroller;
                if (_horizontalScroller.IsEnabled && localPoint.Y >= _insetIdealSize.Y)
                    return _horizontalScroller;
            }

            return base.GetInteractable(point);
        }

        protected internal override void GetUnstyledObjects(ref StyleQueueContext queue)
        {
            if (_verticalScroller.IsEnabled)
                queue.TryEnqueue(_verticalScroller);
            if (_horizontalScroller.IsEnabled)
                queue.TryEnqueue(_horizontalScroller);

            base.GetUnstyledObjects(ref queue);
        }

        public Scroller VerticalScroller => _verticalScroller;
        public Scroller HorizontalScroller => _horizontalScroller;

        public Vector2 InsetIdealSize => _insetIdealSize;
        public Vector2 ScrollPosition { get => _scrollPosition; set => _scrollPosition = value; }

        #region Styleable
        [Styled(nameof(_scrollbars), StateFlags.SelfInvalidLayout)] public Scrollbars Scrollbars { get => _scrollbars; set => SetStyledField(value); }

        [Styled(nameof(_verticalScrollbar), StateFlags.SelfInvalidLayout)] public ScrollbarVisibility VerticalScrollbar { get => _verticalScrollbar; set => SetStyledField(value); }
        [Styled(nameof(_horizontalScrollbar), StateFlags.SelfInvalidLayout)] public ScrollbarVisibility HorizontalScrollbar { get => _horizontalScrollbar; set => SetStyledField(value); }

        [Styled(nameof(_scrollbarWidth), StateFlags.SelfInvalidLayout)] public float ScrollbarWidth { get => _scrollbarWidth; set => SetStyledField(value); }

        [Styled(nameof(_scrollbarBackgroundColor))] public UIColor ScrollbarBackgroundColor { get => _scrollbarBackgroundColor; set => SetStyledField(value); }
        #endregion
    }

    public enum Scrollbars : byte
    { 
        None = 0,
        Vertical = 1 << 0,
        Horizontal = 1 << 1,
        Both = Vertical | Horizontal
    }

    public enum ScrollbarVisibility : byte
    {
        Auto = 0,
        Always,
        Never
    }
}
