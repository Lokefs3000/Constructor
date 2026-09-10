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
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;

namespace EditorUI.Widgets
{
    [UIWidget]
    public class ScrollView : Widget
    {
        [StyleSetup(StateFlags.SelfInvalidLayout)] protected Scrollbars _scrollbars;

        [StyleSetup(StateFlags.SelfInvalidLayout)] protected ScrollbarVisibility _verticalScrollbar;
        [StyleSetup(StateFlags.SelfInvalidLayout)] protected ScrollbarVisibility _horizontalScrollbar;

        [StyleSetup(StateFlags.SelfInvalidLayout)] protected float _scrollbarWidth;

        [StyleInclude] protected UIColor _scrollbarBackgroundColor;

        protected Scroller _verticalScroller;
        protected Scroller _horizontalScroller;

        protected Vector2 _viewSize;
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

            _viewSize = Vector2.Zero;
            _scrollPosition = Vector2.Zero;

            _inputState = WidgetInputState.Intercept;
        }

        protected internal override void DestroySelf()
        {
            _verticalScroller.Destroy();
            _horizontalScroller.Destroy();
            base.DestroySelf();
        }

        // protected internal override MeasureStatus MeasureSelf(ref readonly LayoutContext context)
        // {
        //     MeasureStatus status = base.MeasureSelf(in context);
        //     if (status == MeasureStatus.Success)
        //     {
        //         if ((Flags.HasFlag(_scrollbars, Scrollbars.Vertical) && _verticalScrollbar == ScrollbarVisibility.Always) || _verticalScroller.IsEnabled)
        //             _layoutState.ContentSize.X -= _scrollbarWidth;
        //         if ((Flags.HasFlag(_scrollbars, Scrollbars.Horizontal) && _horizontalScrollbar == ScrollbarVisibility.Always) || _horizontalScroller.IsEnabled)
        //             _layoutState.ContentSize.Y -= _scrollbarWidth;
        //     }
        // 
        //     return MeasureStatus.Success;
        // }

        // protected internal override LayoutReturnData LayoutSelf(ref readonly LayoutContext context)
        // {
        //     base.LayoutSelf(in context);
        // 
        //     bool mayHaveUpdatedContentSize = false;
        // 
        //     Vector2 maxChildExtents = _viewSize;
        //     if (_children != null)
        //     {
        //         foreach (Widget child in _children)
        //         {
        //             maxChildExtents = Vector2.Max(child.IdealPosition + child.IdealSize, maxChildExtents);
        //         }
        // 
        //         _viewSize = Vector2.Max(maxChildExtents, _viewSize);
        //     }
        // 
        //     _verticalScroller.IsEnabled = (_autoResize == AutoResizeMode.None || _autoResize == AutoResizeMode.ResizeX) && Flags.HasFlag(_scrollbars, Scrollbars.Vertical) && (_verticalScrollbar == ScrollbarVisibility.Always || (_verticalScrollbar == ScrollbarVisibility.Auto && maxChildExtents.Y > _layoutState.IdealSize.Y));
        //     _horizontalScroller.IsEnabled = (_autoResize == AutoResizeMode.None || _autoResize == AutoResizeMode.ResizeY) && Flags.HasFlag(_scrollbars, Scrollbars.Horizontal) && (_horizontalScrollbar == ScrollbarVisibility.Always || (_horizontalScrollbar == ScrollbarVisibility.Auto && maxChildExtents.X > _layoutState.IdealSize.X));
        // 
        //     if (_verticalScroller.IsEnabled)
        //     {
        //         if (!_verticalScroller.WasPreviouslyEnabled)
        //         {
        //             mayHaveUpdatedContentSize = true;
        //             _layoutState.ContentSize.X = _layoutState.IdealSize.X - _scrollbarWidth;
        // 
        //             AddStateFlags(_verticalScroller.StateFlags & ~StateFlags.This);
        //         }
        // 
        //         _verticalScroller.FinalizeLayout(_layoutState.ContentSize.Y);
        //     }
        //     else if (_verticalScroller.WasPreviouslyEnabled)
        //     {
        //         UIManager.Instance.InputManager.ForgetInteractable(_verticalScroller);
        //         _verticalScroller.WasPreviouslyEnabled = false;
        //         mayHaveUpdatedContentSize = true;
        //     }
        // 
        //     if (_horizontalScroller.IsEnabled)
        //     {
        //         if (!_horizontalScroller.WasPreviouslyEnabled)
        //         {
        //             mayHaveUpdatedContentSize = true;
        //             _layoutState.ContentSize.Y = _layoutState.IdealSize.Y - _scrollbarWidth;
        // 
        //             AddStateFlags(_verticalScroller.StateFlags & ~StateFlags.This);
        //         }
        // 
        //         _horizontalScroller.FinalizeLayout(_layoutState.ContentSize.X);
        //     }
        //     else if (_horizontalScroller.WasPreviouslyEnabled)
        //     {
        //         UIManager.Instance.InputManager.ForgetInteractable(_horizontalScroller);
        //         _horizontalScroller.WasPreviouslyEnabled = false;
        //         mayHaveUpdatedContentSize = true;
        //     }
        // 
        //     ScrollPosition = Vector2.Clamp(_scrollPosition, Vector2.Zero, Vector2.Max(Vector2.Zero, _viewSize - _layoutState.ContentSize));
        //     _viewSize = Vector2.Max(maxChildExtents, _layoutState.ContentSize);
        // 
        //     if (mayHaveUpdatedContentSize && _children != null && _children.Count > 0)
        //     {
        //         AddStateFlags(StateFlags.SelfInvalidLayout);
        //     }
        // 
        //     return LayoutReturnData.Success;
        // }

        protected internal override void PaintSelf(ref PainterContext painter)
        {
            base.PaintSelf(ref painter);

            if (_verticalScroller.IsEnabled)
            {
                Boundaries boundaries = new Boundaries(
                   new Vector2(_computedRect.Maximum.X - _scrollbarWidth, _computedRect.Minimum.Y),
                   new Vector2(_computedRect.Maximum.X, _computedRect.Minimum.Y + _layoutState.ContentSize.Y));

                if (_scrollbarBackgroundColor.IsVisible)
                    painter.AddRectangle(boundaries, new Paint(_scrollbarBackgroundColor));

                _verticalScroller.PaintSelf(in painter, boundaries);
            }

            if (_horizontalScroller.IsEnabled)
            {
                Boundaries boundaries = new Boundaries(
                 new Vector2(_computedRect.Minimum.X, _computedRect.Maximum.Y - _scrollbarWidth),
                 new Vector2(_computedRect.Minimum.X + _layoutState.ContentSize.X, _computedRect.Maximum.Y));

                if (_scrollbarBackgroundColor.IsVisible)
                    painter.AddRectangle(boundaries, new Paint(_scrollbarBackgroundColor));

                _horizontalScroller.PaintSelf(in painter, boundaries);
            }

            if (_scrollbars != Scrollbars.None)
            {
                painter.PushClippingRect(new Rect(_computedRect.Minimum.AsInt2(), _layoutState.IdealSize.AsInt2()));
                painter.PushTranslate(-Vector2.Round(_scrollPosition));
            }
        }

        public override bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            base.HandleEventSelf(in inputEvent);

            switch (inputEvent.EventType)
            {
                case UIInputEventType.MouseWheel:
                    {
                        const float ScrollSpeed = 50.0f;

                        if (InputSystem.Keyboard.KeyModifiers.HasAny(KeyModifier.Shift))
                        {
                            if (!_horizontalScroller.IsEnabled)
                                return false;

                            float previousX = _scrollPosition.X;
                            _scrollPosition.X = MathF.Round(Math.Clamp(_scrollPosition.X - inputEvent.Mouse.Delta.X * ScrollSpeed, 0.0f, Math.Max(_viewSize.X - _layoutState.ContentSize.X, 0.0f)));

                            if (previousX != _scrollPosition.X)
                                UIManager.Instance.InputManager.ForceInputUpdate();
                        }
                        else
                        {
                            if (!_verticalScroller.IsEnabled)
                                return false;

                            float previousY = _scrollPosition.X;
                            _scrollPosition.Y = MathF.Round(Math.Clamp(_scrollPosition.Y - inputEvent.Mouse.Delta.Y * ScrollSpeed, 0.0f, Math.Max(_viewSize.Y - _layoutState.ContentSize.Y, 0.0f)));

                            if (previousY != _scrollPosition.Y)
                                UIManager.Instance.InputManager.ForceInputUpdate();
                        }

                        return true;
                    }
            }

            return false;
        }

        public override IInteractable GetInteractable(Vector2 point)
        {
            Vector2 localPoint = point - _computedRect.Minimum + Vector2.One;

            bool isAnyOver = Vector2.GreaterThanOrEqualAny(localPoint, _layoutState.ContentSize);
            if (isAnyOver)
            {
                if (_verticalScroller.IsEnabled && localPoint.X >= _layoutState.ContentSize.X)
                    return _verticalScroller;
                if (_horizontalScroller.IsEnabled && localPoint.Y >= _layoutState.ContentSize.Y)
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

        public Vector2 ViewSize => _viewSize;
        public Vector2 ScrollPosition
        {
            get => _scrollPosition;
            set
            {
                if (_scrollPosition != value)
                    UIManager.Instance.InputManager.ForceInputUpdate();
                _scrollPosition = value;
            }
        }

        //public override LayoutBehaviour LayoutBehaviour
        //{
        //    get
        //    {
        //        bool shouldActAsGroup = false;
        //        if (_autoResize != AutoResizeMode.ResizeXY)
        //        {
        //            if (_autoResize == AutoResizeMode.ResizeX)
        //            {
        //                shouldActAsGroup = _verticalScrollbar != ScrollbarVisibility.Never && _scrollbars.HasFlags(Scrollbars.Vertical);
        //            }
        //            else if (_autoResize == AutoResizeMode.ResizeY)
        //            {
        //                shouldActAsGroup = _horizontalScrollbar != ScrollbarVisibility.Never && _scrollbars.HasFlags(Scrollbars.Horizontal);
        //            }
        //            else
        //            {
        //                shouldActAsGroup = _scrollbars != Scrollbars.None && (_verticalScrollbar != ScrollbarVisibility.Never || _horizontalScrollbar != ScrollbarVisibility.Never);
        //            }
        //        }
        //
        //        return shouldActAsGroup ? new LayoutBehaviour(true, true) : LayoutBehaviour.Default;
        //    }
        //}

        #region Styleable
        public Scrollbars Scrollbars { get => _scrollbars; set => SetStyledField(value); }

        public ScrollbarVisibility VerticalScrollbar { get => _verticalScrollbar; set => SetStyledField(value); }
        public ScrollbarVisibility HorizontalScrollbar { get => _horizontalScrollbar; set => SetStyledField(value); }

        public float ScrollbarWidth { get => _scrollbarWidth; set => SetStyledField(value); }

        public UIColor ScrollbarBackgroundColor { get => _scrollbarBackgroundColor; set => SetStyledField(value); }
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
