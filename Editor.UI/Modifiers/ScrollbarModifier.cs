using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Interaction;
using Editor.UI.Layout;
using Editor.UI.Styling;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Mathematics;
using System.Numerics;
using TerraFX.Interop.Windows;

namespace Editor.UI.Modifiers
{
    [ModifierPrettyName("ScrollbarModifier"), StyleableStates("Normal", "Hovered", "Pressed")]
    public class ScrollbarModifier : StyledBaseLayoutModifier, IInteractable, IInteractionShape
    {
        private ScrollbarMode _mode;

        private ScrollbarVisiblity _verticalVis;
        private ScrollbarPosition _verticalPos;

        private ScrollbarVisiblity _horizontalVis;
        private ScrollbarPosition _horizontalPos;

        [PropertyDefault("8.0")] private float _scrollbarWidth;
        [PropertyDefault("0.5 0.5 0.5 1.0")] private UIColor _backgroundColor;
        [PropertyDefault("0.4 0.4 0.4 1.0")] private UIColor _inactiveColor;
        [PropertyDefault("0.7 0.7 0.7 1.0")] private UIColor _scrollbarColor;

        private Boundaries _verticalExtents;
        private Boundaries _horizontalExtents;

        private ScrollbarInteractable _verticalScrollbar;
        private ScrollbarInteractable _horizontalScrollbar;

        private float _currentScroll;

        public ScrollbarModifier(UIElement element) : base(element)
        {
            _mode = ScrollbarMode.Vertical;

            _verticalVis = ScrollbarVisiblity.WhenScrollable;
            _verticalPos = ScrollbarPosition.Right;

            _horizontalVis = ScrollbarVisiblity.WhenScrollable;
            _horizontalPos = ScrollbarPosition.Bottom;

            _scrollbarWidth = 8.0f;
            _backgroundColor = new Color(0.5f);
            _inactiveColor = new Color(0.4f);
            _scrollbarColor = new Color(0.7f);

            _verticalExtents = Boundaries.Zero;
            _horizontalExtents = Boundaries.Zero;

            _verticalScrollbar = new ScrollbarInteractable(this, ScrollbarMode.Vertical);
            _horizontalScrollbar = new ScrollbarInteractable(this, ScrollbarMode.Horizontal);

            _currentScroll = 0.0f;
        }

        public override void ModifyLayout(UILayoutContext context)
        {
            Vector2 sub = _mode switch
            {
                ScrollbarMode.None => Vector2.Zero,
                ScrollbarMode.Vertical => new Vector2(_scrollbarWidth, 0.0f),
                ScrollbarMode.Horizontal => new Vector2(0.0f, _scrollbarWidth),
                ScrollbarMode.Both => new Vector2(_scrollbarWidth),
                _ => throw new NotImplementedException(),
            };

            (bool hasVertical, bool hasHorizontal) = GetScrollbarStates(context.Measurements.ChildExtents - sub);

            if (!hasVertical && !hasHorizontal)
            {
                return;
            }

            _verticalExtents = GetScrollbarExtents(ScrollbarMode.Vertical);
            _horizontalExtents = GetScrollbarExtents(ScrollbarMode.Horizontal);

            if (hasVertical)
            {
                _element.ViewSize = new Vector2(_element.ViewSize.X - _scrollbarWidth, _element.ViewSize.Y);
                if (_verticalPos == ScrollbarPosition.Left)
                    _element.ViewOffset = new Vector2(_element.ViewOffset.X + _scrollbarWidth, _element.ViewOffset.Y);

                if (_element.ChildExtents.X > 0.0f)
                    _element.ChildExtents = new Vector2(_element.ChildExtents.X - _scrollbarWidth, _element.ChildExtents.Y);
            }
            if (hasHorizontal)
            {
                _element.ViewSize = new Vector2(_element.ViewSize.X, _element.ViewSize.Y - _scrollbarWidth);
                if (_verticalPos == ScrollbarPosition.Left)
                    _element.ViewOffset = new Vector2(_element.ViewOffset.X, _element.ViewOffset.Y + _scrollbarWidth);

                if (_element.ChildExtents.Y > 0.0f)
                    _element.ChildExtents = new Vector2(_element.ChildExtents.X, _element.ChildExtents.Y - _scrollbarWidth);
            }

            if (hasVertical && hasHorizontal)
            {
                // subtract from previous extents
                if (_verticalPos == ScrollbarPosition.Right)
                    _horizontalExtents.Maximum.X -= _scrollbarWidth;
                else
                    _horizontalExtents.Minimum.X += _scrollbarWidth;

                if (_horizontalPos == ScrollbarPosition.Bottom)
                    _verticalExtents.Maximum.Y -= _scrollbarWidth;
                else
                    _verticalExtents.Minimum.Y -= _scrollbarWidth;
            }
        }

        public override void ModifyVisual(UIPainterContext painter)
        {
            (bool vert, bool hori) = GetScrollbarStates(_element.ChildExtents);

            if (!vert && !hori)
                return;

            UIPaint bgColor = UIPaint.FromColor(_backgroundColor);
            UIPaint fgColor = UIPaint.FromColor(_scrollbarColor);
            UIPaint inactiveFgColor = UIPaint.FromColor(_inactiveColor);

            Boundaries vExtents = Boundaries.Offset(_verticalExtents, _element.PixelCoordinates.Minimum);
            Boundaries hExtents = Boundaries.Offset(_horizontalExtents, _element.PixelCoordinates.Minimum);

            if (vert && hori)
            {
                Vector2 position = new Vector2(vExtents.Minimum.X, hExtents.Minimum.Y);
                painter.DrawRect(new Boundaries(position, position + new Vector2(_scrollbarWidth)), bgColor);
            }

            if (vert)
            {
                float scrollbarHeight = _element.ViewSize.Y / _element.ChildExtents.Y;
                if (scrollbarHeight < 1.0f)
                {
                    float scrollbarOffset = Math.Clamp(_element.ScrollPosition.Y, 0.0f, _element.ChildExtents.Y - _element.ViewSize.Y) * scrollbarHeight;

                    Boundaries scrollbarExtents = _verticalExtents;
                    scrollbarExtents.Maximum.Y *= scrollbarHeight;
                    scrollbarExtents = Boundaries.Offset(scrollbarExtents, _element.PixelCoordinates.Minimum + new Vector2(0.0f, scrollbarOffset));

                    painter.DrawRect(vExtents, bgColor);
                    painter.DrawRect(scrollbarExtents, fgColor, 0.15f);
                }
                else
                {
                    painter.DrawRect(vExtents, inactiveFgColor, 0.15f);
                }
            }

            if (hori)
            {
                float scrollbarHeight = _element.ViewSize.X / _element.ChildExtents.X;
                if (scrollbarHeight < 1.0f)
                {
                    float scrollbarOffset = Math.Clamp(_element.ScrollPosition.X, 0.0f, _element.ChildExtents.X - _element.ViewSize.X) * scrollbarHeight;

                    Boundaries scrollbarExtents = _horizontalExtents;
                    scrollbarExtents.Maximum.X *= scrollbarHeight;
                    scrollbarExtents = Boundaries.Offset(scrollbarExtents, _element.PixelCoordinates.Minimum + new Vector2(scrollbarOffset, 0.0f));

                    painter.DrawRect(hExtents, bgColor);
                    painter.DrawRect(scrollbarExtents, fgColor, 0.15f);
                }
                else
                {
                    painter.DrawRect(hExtents, inactiveFgColor, 0.15f);
                }
            }
        }

        public void HandleEvent(ref readonly UIEvent @event)
        {

        }

        public void HandleScrollbarEvent(ref readonly UIEvent @event, ScrollbarMode mode)
        {
            switch (@event.Type)
            {
                case UIEventType.MouseEnter: SetState("Hovered", true); break;
                case UIEventType.MouseLeave: SetState("Hovered", false); break;
                case UIEventType.MouseDown: SetState("Pressed", true); break;
                case UIEventType.MouseUp: SetState("Pressed", false); break;
                case UIEventType.DragBegin:
                    {
                        _currentScroll = mode == ScrollbarMode.Horizontal ? _element.ScrollPosition.X : _element.ScrollPosition.Y;
                        break;
                    }
                case UIEventType.DragUpdate:
                    {
                        if (mode == ScrollbarMode.Horizontal)
                        {
                            float scrollbarHeight = _element.ViewSize.X / _element.ChildExtents.X;
                            float maxScrollX = _element.ChildExtents.X - _element.ViewSize.X;

                            if (maxScrollX <= 0.0f)
                                _element.ScrollPosition = new Vector2(0.0f, _element.ScrollPosition.Y);
                            else
                                _element.ScrollPosition = new Vector2(Math.Clamp(_currentScroll + @event.Drag.Delta.X / scrollbarHeight, 0.0f, maxScrollX), _element.ScrollPosition.Y);
                        }
                        else
                        {
                            float scrollbarHeight = _element.ViewSize.Y / _element.ChildExtents.Y;
                            float maxScrollY = _element.ChildExtents.Y - _element.ViewSize.Y;

                            if (maxScrollY <= 0.0f)
                                _element.ScrollPosition = new Vector2(_element.ScrollPosition.X, 0.0f);
                            else
                                _element.ScrollPosition = new Vector2(_element.ScrollPosition.X, Math.Clamp(_currentScroll + @event.Drag.Delta.Y / scrollbarHeight, 0.0f, maxScrollY));
                        }

                        break;
                    }
            }
        }

        public IInteractable GetInteractable(Vector2 point)
        {
            (bool vert, bool hori) = GetScrollbarStates(_element.ChildExtents);

            point -= _element.PixelCoordinates.Minimum;

            if (vert)
            {
                bool isWithin = _verticalPos == ScrollbarPosition.Right ?
                    point.X >= _verticalExtents.Minimum.X :
                    point.X <= _verticalExtents.Maximum.X;

                if (isWithin)
                {
                    float scrollbarHeight = _element.ViewSize.Y / _element.ChildExtents.Y;
                    if (scrollbarHeight >= 1.0f)
                        return _verticalScrollbar;

                    float scrollbarOffset = Math.Clamp(_element.ScrollPosition.Y, 0.0f, _element.ChildExtents.Y - _element.ViewSize.Y) * scrollbarHeight;

                    float top = _verticalExtents.Minimum.Y + scrollbarOffset;
                    float bottom = _verticalExtents.Minimum.Y + scrollbarOffset + _verticalExtents.Maximum.Y * scrollbarHeight;

                    return point.Y >= top && point.Y <= bottom ? _verticalScrollbar : this;
                }
            }

            if (hori)
            {
                bool isWithin = _horizontalPos == ScrollbarPosition.Bottom ?
                    point.Y >= _horizontalExtents.Minimum.Y :
                    point.Y <= _horizontalExtents.Maximum.Y;

                if (isWithin)
                {
                    float scrollbarHeight = _element.ViewSize.X / _element.ChildExtents.X;
                    if (scrollbarHeight >= 1.0f)
                        return _horizontalScrollbar;

                    float scrollbarOffset = Math.Clamp(_element.ScrollPosition.X, 0.0f, _element.ChildExtents.X - _element.ViewSize.X) * scrollbarHeight;

                    float left = _horizontalExtents.Minimum.X + scrollbarOffset;
                    float right = _horizontalExtents.Minimum.X + scrollbarOffset + _horizontalExtents.Maximum.X * scrollbarHeight;

                    return point.X >= left && point.X <= right ? _horizontalScrollbar : this;
                }
            }

            return this;
        }

        public bool Intersects(Vector2 point)
        {
            (bool vert, bool hori) = GetScrollbarStates(_element.ChildExtents);

            point -= _element.PixelCoordinates.Minimum;

            if (vert)
            {
                if (_verticalPos == ScrollbarPosition.Right)
                {
                    if (point.X >= _verticalExtents.Minimum.X)
                        return true;
                }
                else
                {
                    if (point.X <= _verticalExtents.Maximum.X)
                        return true;
                }
            }

            if (hori)
            {
                if (_horizontalPos == ScrollbarPosition.Bottom)
                {
                    if (point.Y >= _horizontalExtents.Minimum.Y)
                        return true;
                }
                else
                {
                    if (point.Y <= _horizontalExtents.Maximum.Y)
                        return true;
                }
            }

            return false;
        }

        private (bool vert, bool hori) GetScrollbarStates(Vector2 extents)
        {
            bool hasVertical = Flags.HasFlag(_mode, ScrollbarMode.Vertical);
            if (hasVertical)
            {
                if (_verticalVis == ScrollbarVisiblity.WhenScrollable)
                    hasVertical = extents.Y > _element.ViewSize.Y;
            }

            bool hasHorizontal = Flags.HasFlag(_mode, ScrollbarMode.Horizontal);
            if (hasHorizontal)
            {
                if (_horizontalVis == ScrollbarVisiblity.WhenScrollable)
                    hasHorizontal = extents.Y > _element.ViewSize.Y;
            }

            return (hasVertical, hasHorizontal);
        }

        private Boundaries GetScrollbarExtents(ScrollbarMode direction)
        {
            Boundaries extents;
            if (direction == ScrollbarMode.Vertical)
            {
                if (_verticalPos == ScrollbarPosition.Right)
                    extents = new Boundaries(new Vector2(_element.CurrentSize.X - _scrollbarWidth, 0.0f), _element.CurrentSize);
                else
                    extents = new Boundaries(Vector2.Zero, new Vector2(_scrollbarWidth, _element.CurrentSize.Y));
            }
            else
            {
                if (_horizontalPos == ScrollbarPosition.Bottom)
                    extents = new Boundaries(new Vector2(0.0f, _element.CurrentSize.Y - _scrollbarWidth), _element.CurrentSize);
                else
                    extents = new Boundaries(Vector2.Zero, new Vector2(_element.CurrentSize.X, _scrollbarWidth));
            }

            return extents;
        }

        public IWindowHost? Host => _element.Host;
        public IInteractionShape? Shape => this;

        protected override StyleUpdater? StyleUpdater => _element.WindowOwner?.StyleUpdater;
        protected override StyleProvider? StyleProvider => _element.WindowOwner?.StyleProvider;

        #region Properties
        [EditableProperty(nameof(_mode), UIStateFlags.InvalidAll)] public ScrollbarMode Mode { get => _mode; set => SetEditableProperty(value); }

        [EditableProperty(nameof(_verticalVis), UIStateFlags.InvalidAll)] public ScrollbarVisiblity VerticalVisiblity { get => _verticalVis; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_verticalPos), UIStateFlags.InvalidAll)] public ScrollbarPosition VerticalPosition { get => _verticalPos; set => SetEditableProperty(value); }

        [EditableProperty(nameof(_horizontalVis), UIStateFlags.InvalidAll)] public ScrollbarVisiblity HorizontalVisibility { get => _horizontalVis; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_horizontalPos), UIStateFlags.InvalidAll)] public ScrollbarPosition HorizontalPosition { get => _horizontalPos; set => SetEditableProperty(value); }

        [StyleableProperty(nameof(_scrollbarWidth), UIStateFlags.InvalidAll)] public float ScrollbarWidth { get => _scrollbarWidth; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_backgroundColor), UIStateFlags.InvalidVisual)] public UIColor BackgroundColor { get => _backgroundColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_scrollbarColor), UIStateFlags.InvalidVisual)] public UIColor ScrollbarColor { get => _scrollbarColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_inactiveColor), UIStateFlags.InvalidVisual)] public UIColor InactiveColor { get => _inactiveColor; set => SetStyleProperty(value); }
        #endregion

        private record class ScrollbarInteractable(ScrollbarModifier Owner, ScrollbarMode Mode) : IInteractable
        {
            public IInteractable GetInteractable(Vector2 point) => this;

            public void HandleEvent(ref readonly UIEvent @event) => Owner.HandleScrollbarEvent(in @event, Mode);

            public IWindowHost? Host => Owner._element.Host;
            public IInteractionShape? Shape => null;
        }
    }

    [Flags]
    public enum ScrollbarMode : byte
    {
        None = 0,

        Vertical = 1 << 0,
        Horizontal = 1 << 1,

        Both = Vertical | Horizontal
    }

    public enum ScrollbarVisiblity : byte
    {
        WhenScrollable = 0,
        Always
    }

    public enum ScrollbarPosition : byte
    {
        Right = 0,
        Left,

        Bottom = 0,
        Top
    }
}
