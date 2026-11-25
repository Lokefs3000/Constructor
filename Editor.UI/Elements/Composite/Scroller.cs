using Editor.UI.Datatypes;
using Editor.UI.Interaction;
using Editor.UI.Styling;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements.Composite
{
    [StyleableStates("Normal", "Hovered", "Pressed", "Inactive")]
    public class Scroller : StyleBase, IInteractable
    {
        protected readonly UIScrollView _scrollView;
        protected readonly Handle _handle;

        protected ScrollerDirection _direction;
        protected float _scroll;

        [PropertyDefault("0.5 0.5 0.5 1.0")] protected UIColor _backgroundColor;
        [PropertyDefault("0.8 0.8 0.8 1.0")] protected UIColor _foregroundColor;
        [PropertyDefault("6.0")] protected float _width;

        [PropertyDefault("0.0")] protected float _cornerRadius;

        protected Boundaries _boundaries;
        private float _startScroll;

        public Scroller(UIScrollView scrollView)
        {
            _scrollView = scrollView;
            _handle = new Handle(this);

            _direction = ScrollerDirection.Vertical;
            _scroll = 0.0f;

            _backgroundColor = new Color(0.5f);
            _foregroundColor = new Color(0.7f);
            _width = 6.0f;

            _cornerRadius = 0.0f;

            _boundaries = Boundaries.Zero;
            _startScroll = 0.0f;
        }

        public void Destroy()
        {
            DestroySelf();

            StyleUpdater?.RemoveInvalidStyleBase(this);

            _scrollView?.WindowOwner?.ParentHost?.InteractionManager?.DereferenceDestroyedInteractable(this);
            _scrollView?.WindowOwner?.ParentHost?.InteractionManager?.DereferenceDestroyedInteractable(_handle);
        }

        protected virtual void DestroySelf() { }

        public virtual void FinalizeLayout(bool isOtherScrollerVisible = false)
        {
            if (_direction == ScrollerDirection.Vertical)
            {
                _boundaries = new Boundaries(
                    new Vector2(_scrollView.PixelCoordinates.Maximum.X - _width, _scrollView.PixelCoordinates.Minimum.Y),
                    _scrollView.PixelCoordinates.Maximum);

                if (isOtherScrollerVisible)
                    _boundaries.Maximum.Y -= _scrollView.Horizontal.Width;
                _scroll = Math.Min(_scroll, Math.Max(_scrollView.ChildExtents.Y - _scrollView.ViewSize.Y, 0.0f));

                SetState("Inactive", _scrollView.ViewSize.Y >= _scrollView.ChildExtents.Y && _scrollView.VerticalVisiblity == ScrollbarVisibility.AlwaysVisible);
            }
            else
            {
                _boundaries = new Boundaries(
                   new Vector2(_scrollView.PixelCoordinates.Minimum.X, _scrollView.PixelCoordinates.Maximum.Y - _width),
                   _scrollView.PixelCoordinates.Maximum);

                if (isOtherScrollerVisible)
                    _boundaries.Maximum.X -= _scrollView.Vertical.Width;
                _scroll = Math.Min(_scroll, Math.Max(_scrollView.ChildExtents.X - _scrollView.ViewSize.X, 0.0f));

                SetState("Inactive", _scrollView.ViewSize.X >= _scrollView.ChildExtents.X && _scrollView.HorizontalVisiblity == ScrollbarVisibility.AlwaysVisible);
            }
        }

        public virtual void DrawVisual(UIPainterContext painter)
        {
            _scroll = _direction == ScrollerDirection.Vertical ? _scrollView.ScrollPosition.Y : _scrollView.ScrollPosition.X;

            (float relativeHeight, float handleWidth, float handleOffset) = GetHandleMetrics();

            if (relativeHeight >= 1.0f)
            {
                if (_cornerRadius > 0.5f - float.Epsilon)
                    painter.DrawRect(_boundaries, UIPaint.FromColor(_backgroundColor));
                painter.DrawRect(_boundaries, UIPaint.FromColor(_foregroundColor), _cornerRadius);
            }
            else
            {
                painter.DrawRect(_boundaries, UIPaint.FromColor(_backgroundColor));

                Boundaries handleBoundaries = _boundaries;
                if (_direction == ScrollerDirection.Vertical)
                {
                    handleBoundaries.Minimum.Y += handleOffset;
                    handleBoundaries.Maximum.Y = handleBoundaries.Minimum.Y + handleWidth;
                }
                else
                {
                    handleBoundaries.Minimum.X += handleOffset;
                    handleBoundaries.Maximum.X = handleBoundaries.Minimum.X + handleWidth;
                }

                painter.DrawRect(handleBoundaries, UIPaint.FromColor(_foregroundColor), _cornerRadius);
            }
        }

        public virtual void HandleEvent(ref readonly UIEvent @event)
        {
            switch (@event.Type)
            {
                case UIEventType.MouseWheel: _scrollView.HandleEvent(in @event); break;
            }
        }

        private void HandleHandleEvent(ref readonly UIEvent @event)
        {
            switch (@event.Type)
            {
                case UIEventType.MouseWheel: _scrollView.HandleEvent(in @event); break;
                case UIEventType.MouseEnter: SetState("Hovered", true); break;
                case UIEventType.MouseLeave: SetState("Hovered", false); break;
                case UIEventType.MouseDown: SetState("Pressed", true); break;
                case UIEventType.MouseUp: SetState("Pressed", false); break;
                case UIEventType.DragBegin: _startScroll = _scroll; break;
                case UIEventType.DragUpdate:
                    {
                        if (_direction == ScrollerDirection.Vertical)
                        {
                            float localScroll = Math.Clamp(_startScroll + @event.Drag.Delta.Y / (_scrollView.ViewSize.Y / _scrollView.ChildExtents.Y), 0.0f, GetMaxScroll());
                            if (_scroll != localScroll)
                            {
                                _scroll = localScroll;
                                OnScroll?.Invoke(_scroll);
                            }
                        }
                        else
                        {
                            float localScroll = Math.Clamp(_startScroll + @event.Drag.Delta.X / (_scrollView.ViewSize.X / _scrollView.ChildExtents.X), 0.0f, GetMaxScroll());
                            if (_scroll != localScroll)
                            {
                                _scroll = localScroll;
                                OnScroll?.Invoke(_scroll);
                            }
                        }

                        break;
                    }
            }
        }

        private (float relativeHeight, float handleWidth, float handleOffset) GetHandleMetrics()
        {
            float scrollerHeight = _direction == ScrollerDirection.Vertical ?
                _scrollView.ViewSize.Y / _scrollView.ChildExtents.Y :
                _scrollView.ViewSize.X / _scrollView.ChildExtents.X;

            if (_direction == ScrollerDirection.Vertical)
            {
                float height = (_boundaries.Maximum.Y - _boundaries.Minimum.Y) * scrollerHeight;
                float offset = float.Lerp(_boundaries.Minimum.Y, _boundaries.Maximum.Y - height, _scroll / (_scrollView.ChildExtents.Y - _scrollView.ViewSize.Y)) - _boundaries.Minimum.Y;

                return (scrollerHeight, height, offset);
            }
            else
            {
                float width = (_boundaries.Maximum.X - _boundaries.Minimum.X) * scrollerHeight;
                float offset = float.Lerp(_boundaries.Minimum.X, _boundaries.Maximum.X - width, _scroll / (_scrollView.ChildExtents.X - _scrollView.ViewSize.X)) - _boundaries.Minimum.X;

                return (scrollerHeight, width, offset);
            }
        }

        private float GetMaxScroll() => _direction == ScrollerDirection.Vertical ?
            Math.Max(_scrollView.ChildExtents.Y - _scrollView.ViewSize.Y, 0.0f) :
            Math.Max(_scrollView.ChildExtents.X - _scrollView.ViewSize.X, 0.0f);

        public IInteractable GetInteractable(Vector2 point)
        {
            if (_direction == ScrollerDirection.Vertical)
            {
                (float relativeHeight, float handleWidth, float handleOffset) = GetHandleMetrics();

                handleOffset += _boundaries.Minimum.Y;
                return (point.Y >= handleOffset && point.Y <= handleOffset + handleWidth) ? _handle : this;
            }
            else
            {
                (float relativeHeight, float handleWidth, float handleOffset) = GetHandleMetrics();

                handleOffset += _boundaries.Minimum.X;
                return (point.X >= handleOffset && point.X <= handleOffset + handleWidth) ? _handle : this;
            }
        }

        public IWindowHost? Host => _scrollView.Host;
        public IInteractionShape? Shape => null;

        protected override StyleUpdater? StyleUpdater => _scrollView.WindowOwner?.StyleUpdater;
        protected override StyleProvider? StyleProvider => _scrollView.WindowOwner?.StyleProvider;

        public Boundaries ScrollerBoundaries => _boundaries;

        public float Value
        {
            get => _scroll; set
            {
                _scroll = Math.Clamp(value, 0.0f, Math.Max(_scrollView.ChildExtents.Y - _scrollView.ViewSize.Y, 0.0f));
                _scrollView.AddStateFlags(UIStateFlags.InvalidVisual);
            }
        }

        #region Properties
        [EditableProperty(nameof(_direction), UIStateFlags.InvalidAll)] public ScrollerDirection Direction { get => _direction; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_scroll), UIStateFlags.InvalidLayout)] public float Scroll { get => _scroll; set { SetEditableProperty(Math.Clamp(value, 0.0f, GetMaxScroll())); OnScroll?.Invoke(_scroll); } }

        [StyleableProperty(nameof(_backgroundColor), UIStateFlags.InvalidVisual)] public UIColor BackgroundColor { get => _backgroundColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_foregroundColor), UIStateFlags.InvalidVisual)] public UIColor ForegroundColor { get => _foregroundColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_width), UIStateFlags.InvalidAll)] public float Width { get => _width; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_cornerRadius), UIStateFlags.InvalidVisual)] public float CornerRadius { get => _width; set => SetStyleProperty(value); }
        #endregion
        #region Events
        public event Action<float>? OnScroll;
        #endregion

        protected sealed record class Handle(Scroller Owner) : IInteractable
        {
            public IInteractable GetInteractable(Vector2 point) => this;
            public void HandleEvent(ref readonly UIEvent @event) => Owner.HandleHandleEvent(in @event);

            public IInteractionShape? Shape => null;
            public IWindowHost? Host => Owner._scrollView.Host;
        }
    }

    public enum ScrollerDirection : byte
    {
        Vertical = 0,
        Horizontal
    }
}
