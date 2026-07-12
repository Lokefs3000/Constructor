using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using CommunityToolkit.Diagnostics;
using EditorUI.Common;
using EditorUI.Input;
using EditorUI.Layout;
using EditorUI.Mathematics;
using EditorUI.Styling;
using EditorUI.Visual;
using EditorUI.Widgets.Stylists;
using Primary;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.Utility;

namespace EditorUI.Widgets
{
    [UIWidget]
    public class Widget : StyledObject, IInteractable, IInteractionShape
    {
        protected bool _isDestroyed;

        protected string? _id;

        protected bool _isEnabled;
        protected WidgetInputState _inputState;

        protected Widget? _parent;
        protected List<Widget>? _children;
        protected List<Stylist>? _stylists;

        protected UIValue2 _position;
        protected UIValue2 _size;

        protected Vector2 _anchor;

        protected Vector4 _margin;
        protected Vector4 _padding;

        protected AutoResizeMode _autoResize;

        protected UIColor _backgroundColor;
        protected Vector4 _cornerRadius;

        protected UIColor _strokeColor;
        protected StrokePosition _strokePosition;
        protected ushort _strokeWidth;

        // internal state
        protected Vector2 _idealPosition;
        protected Vector2 _idealSize;
        protected Vector2 _viewSize;
        protected Boundaries _computedRect;

        protected StateFlags _stateFlags;

        public Widget()
        {
            _id = null;

            _isEnabled = true;
            _inputState = WidgetInputState.Sink;

            _parent = null;
            _children = null;
            _stylists = null;

            _position = UIValue2.Zero;
            _size = UIValue2.Zero;

            _anchor = Vector2.Zero;

            _margin = Vector4.Zero;
            _padding = Vector4.Zero;

            _autoResize = AutoResizeMode.None;

            _backgroundColor = Color.TransparentBlack;
            _cornerRadius = Vector4.Zero;

            _strokeColor = Color.TransparentBlack;
            _strokePosition = StrokePosition.Outside;
            _strokeWidth = 0;

            _idealPosition = Vector2.Zero;
            _idealSize = Vector2.Zero;
            _viewSize = Vector2.Zero;
            _computedRect = Boundaries.Zero;

            _stateFlags = StateFlags.SelfInvalidLayout | StateFlags.SelfInvalidStyle;
        }

        #region Methods
        public void Destroy()
        {
            WidgetManager.Instance.QueueDestroy(this);
        }

        protected internal virtual void DestroySelf()
        {
            UIManager.Instance.InputManager.ForgetInteractable(this);
            ChangeActiveParent(null);
            _isDestroyed = true;

            if (_children != null)
            {
                while (_children.Count > 0)
                {
                    _children[0].DestroySelf();
                }
            }
        }

        protected internal virtual MeasureReturnData MeasureSelf(ref readonly LayoutContext context)
        {
            if (!context.LayoutLock.HasFlags(LayoutLockAxis.AxisX))
            {
                if (_autoResize == AutoResizeMode.ResizeX || _autoResize == AutoResizeMode.ResizeXY)
                    _idealSize.X = float.NegativeZero;
                else
                    _idealSize.X = _size.X.Evaluate(context.ParentSize.X);
            }

            if (!context.LayoutLock.HasFlags(LayoutLockAxis.AxisY))
            {
                if (_autoResize == AutoResizeMode.ResizeY || _autoResize == AutoResizeMode.ResizeXY)
                    _idealSize.Y = float.NegativeZero;
                else
                    _idealSize.Y = _size.Y.Evaluate(context.ParentSize.Y);
            }

            return MeasureReturnData.Success;
        }

        protected internal virtual LayoutReturnData LayoutSelf(ref readonly LayoutContext context)
        {
            if (!context.LayoutLock.HasFlags(LayoutLockAxis.AxisX))
                _idealPosition.X = _position.X.Evaluate(context.ParentSize.X);
            if (!context.LayoutLock.HasFlags(LayoutLockAxis.AxisY))
                _idealPosition.Y = _position.Y.Evaluate(context.ParentSize.Y);

            if (!_anchor.Equals(Vector2.Zero))
                _idealPosition -= _anchor * _idealSize;
            return LayoutReturnData.Success;
        }

        protected internal virtual void PostLayoutSelf(ref readonly LayoutContext context)
        {
        }

        protected internal virtual void FinalizeSelf(ref readonly LayoutContext context)
        {
        }

        protected internal virtual void PaintSelf(ref PainterContext context)
        {
            if (_backgroundColor.IsVisible || (_strokeWidth > 0 && _strokeColor.IsVisible))
            {
                Paint paint = new Paint(_backgroundColor, _strokeColor, _strokeWidth, _strokePosition);
                if (_cornerRadius.X < 1.0f)
                    context.AddRectangle(_computedRect, paint);
                else
                    context.AddRectangle(_computedRect, paint, _cornerRadius);
            }
        }

        public virtual void HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            switch (inputEvent.EventType)
            {
                case UIInputEventType.MouseMotion: OnMouseMotion?.Invoke(this, inputEvent.Mouse); break;
                case UIInputEventType.MouseWheel: OnMouseWheel?.Invoke(this, inputEvent.Mouse); break;
                case UIInputEventType.MouseEnter: OnMouseHover?.Invoke(this, inputEvent.Mouse, true); break;
                case UIInputEventType.MouseLeave: OnMouseHover?.Invoke(this, inputEvent.Mouse, false); break;
                case UIInputEventType.MouseDown: OnMouseButton?.Invoke(this, inputEvent.Mouse, true); break;
                case UIInputEventType.MouseUp: OnMouseButton?.Invoke(this, inputEvent.Mouse, false); break;
                case UIInputEventType.MousePress: OnMousePress?.Invoke(this, inputEvent.Mouse); break;

                case UIInputEventType.DragBegin: OnDragBegin?.Invoke(this, inputEvent.Drag); break;
                case UIInputEventType.DragUpdate: OnDragUpdate?.Invoke(this, inputEvent.Drag); break;
                case UIInputEventType.DragEnd: OnDragEnd?.Invoke(this, inputEvent.Drag); break;

                case UIInputEventType.KeyDown: OnKey?.Invoke(this, inputEvent.Key, true); break;
                case UIInputEventType.KeyUp: OnKey?.Invoke(this, inputEvent.Key, false); break;
            }
        }
        #endregion

        #region Relationships
        protected virtual void ChangeActiveParent(Widget? newParent)
        {
            if (newParent == _parent)
                return;

            if (_parent != null)
            {
                _parent.TryRemoveChild(this);
                _parent = null;
            }

            if (newParent != this && newParent != null)
            {
                newParent.TryAddChild(this);
                _parent = newParent;

                AddStateFlags(StateFlags.SelfInvalidLayout | (_stateFlags & ~StateFlags.This));
            }
        }

        private bool TryAddChild(Widget child)
        {
            return (_children ??= new List<Widget>()).AddUnique(child);
        }

        private bool TryRemoveChild(Widget child)
        {
            return _children?.Remove(child) ?? false;
        }

        public void AddChild(Widget widget) => widget.Parent = this;

        public void TryMoveChild(Widget widget, int newChildIndex)
        {
            if (_children != null && _children.Count > 1 && newChildIndex >= 0 && newChildIndex <= _children.Count)
            {
                int indexOf = _children.IndexOf(widget);

                _children.RemoveAt(indexOf);
                _children.Insert(newChildIndex - 1, widget);
            }
        }

        public T? FindWidgetWithId<T>(string id, bool throwIfNotFound = false) where T : Widget
        {
            T? found = RecursiveDescentFindId<T>(id);
            if (found == null && throwIfNotFound)
                throw new Exception(id);
            return found;
        }

        private T? RecursiveDescentFindId<T>(string id) where T : Widget
        {
            if (this is T t && _id == id)
                return t;

            if (_children == null)
                return null;

            foreach (Widget child in _children)
            {
                T? ret = child.FindWidgetWithId<T>(id);
                if (ret != null)
                    return ret;
            }

            return null;
        }
        #endregion

        #region Stylists
        public virtual T CreateStylist<T>() where T : Stylist
        {
            T stylist = (T)Activator.CreateInstance(typeof(T), [this])!;
            (_stylists ??= new List<Stylist>()).Add(stylist);

            OnStylistCreated?.Invoke(this, stylist);
            return stylist;
        }

        public virtual Stylist CreateStylist(Type type)
        {
            Guard.IsTrue(type.IsAssignableTo(typeof(Stylist)));

            Stylist stylist = (Stylist)Activator.CreateInstance(type, [this])!;
            (_stylists ??= new List<Stylist>()).Add(stylist);

            AddStateFlags(StateFlags.InvalidStyle);

            OnStylistCreated?.Invoke(this, stylist);
            return stylist;
        }

        public virtual void DestroyStylist(Stylist stylist)
        {
            if (_stylists?.Remove(stylist) ?? false)
            {
                OnStylistDestroyed?.Invoke(this, stylist);
            }
        }

        public T? FindStylist<T>(string? id = null) where T : Stylist
        {
            if (_stylists == null || _stylists.Count == 0)
                return null;

            if (id != null)
            {
                foreach (Stylist stylist in _stylists)
                {
                    if (stylist is T t && stylist.Id == id)
                    {
                        return t;
                    }
                }

                return null;
            }
            else
            {
                foreach (Stylist stylist in _stylists)
                {
                    if (stylist is T t)
                    {
                        return t;
                    }
                }

                return null;
            }
        }

        internal void InformOfStylistUpdate(Stylist stylist)
        {
            OnStylistUpdated?.Invoke(this, stylist);
        }

        internal void InformOfStylistIdChange(Stylist stylist)
        {
            OnStylistIdChanged?.Invoke(this, stylist);
        }
        #endregion

        #region State
        public override void AddStateFlags(StateFlags flags)
        {
            _stateFlags |= flags;

            // ripple upwards
            if (_parent != null)
            {
                flags &= ~StateFlags.This;
                if (flags != StateFlags.None && !_parent._stateFlags.HasFlags(flags))
                {
                    _parent.AddStateFlags(flags);
                }
            }
        }

        public override void RemoveStateFlags(StateFlags flags)
        {
            _stateFlags &= ~flags;
        }
        #endregion

        #region Input
        public virtual IInteractable GetInteractable(Vector2 point) => this;
        #endregion

        public virtual LayoutBehaviour LayoutBehaviour => LayoutBehaviour.Default;

        public bool IsDestroyed => _isDestroyed;

        public Widget? Parent { get => _parent; set => ChangeActiveParent(value); }
        public ROList<Widget> Children => _children ?? ROList<Widget>.Empty;
        public ROList<Stylist> Stylists => _stylists ?? ROList<Stylist>.Empty;

        public Vector2 IdealSize { get => _idealSize; protected internal set => _idealSize = value; }
        public Vector2 IdealPosition { get => _idealPosition; protected internal set => _idealPosition = value; }
        public Vector2 ViewSize { get => _viewSize; protected internal set => _viewSize = value; }
        public Boundaries ComputedRect { get => _computedRect; protected internal set => _computedRect = value; }

        public override StateFlags StateFlags => _stateFlags;

        #region StyledObject
        protected internal override void GetUnstyledObjects(ref StyleQueueContext queue)
        {
            if (_stylists != null)
            {
                foreach (Stylist stylist in _stylists)
                {
                    queue.TryEnqueue(stylist);
                }
            }

            if (_children != null)
            {
                foreach (Widget child in _children)
                {
                    queue.TryEnqueue(child);
                }
            }
        }

        protected internal override StyledObject? ParentObject => _parent;
        #endregion

        #region IInteractable
        public virtual IInteractionShape? Shape => this;
        #endregion

        #region IInteractionShape
        public virtual bool Intersects(Vector2 point)
        {
            return _computedRect.IsWithin(point);
        }
        #endregion

        #region Events
        public event WidgetEventHandler<UIMouseInputEvent>? OnMouseMotion;
        public event WidgetEventHandler<UIMouseInputEvent>? OnMouseWheel;
        public event WidgetEventHandler<UIMouseInputEvent, bool>? OnMouseHover;
        public event WidgetEventHandler<UIMouseInputEvent, bool>? OnMouseButton;
        public event WidgetEventHandler<UIMouseInputEvent>? OnMousePress;

        public event WidgetEventHandler<UIDragInputEvent>? OnDragBegin;
        public event WidgetEventHandler<UIDragInputEvent>? OnDragUpdate;
        public event WidgetEventHandler<UIDragInputEvent>? OnDragEnd;

        public event WidgetEventHandler<UIKeyInputEvent, bool>? OnKey;

        public event WidgetEventHandler<Stylist>? OnStylistCreated;
        public event WidgetEventHandler<Stylist>? OnStylistDestroyed;
        public event WidgetEventHandler<Stylist>? OnStylistUpdated;
        public event WidgetEventHandler<Stylist>? OnStylistIdChanged;
        #endregion

        #region Serializable
        [Styled(nameof(_id), isEditable: true)] public string? Id { get => _id; set => SetEditedField(value); }

        [Styled(nameof(_isEnabled), StateFlags.SelfInvalidLayout, true, true)] public bool IsEnabled { get => _isEnabled; set => SetEditedField(value); }
        [Styled(nameof(_inputState), isEditable: true)] public WidgetInputState InputState { get => _inputState; set => SetEditedField(value); }

        [Styled(nameof(_position), StateFlags.SelfInvalidLayout)] public UIValue2 Position { get => _position; set => SetStyledField(value); }
        [Styled(nameof(_size), StateFlags.SelfInvalidLayout)] public UIValue2 Size { get => _size; set => SetStyledField(value); }

        [Styled(nameof(_anchor), StateFlags.SelfInvalidLayout)] public Vector2 Anchor { get => _anchor; set => SetStyledField(value); }

        [Styled(nameof(_padding), StateFlags.SelfInvalidLayout)] public Vector4 Padding { get => _padding; set => SetStyledField(value); }
        [Styled(nameof(_margin), StateFlags.SelfInvalidLayout)] public Vector4 Margin { get => _margin; set => SetStyledField(value); }

        [Styled(nameof(_autoResize), StateFlags.SelfInvalidLayout)] public AutoResizeMode AutoResize { get => _autoResize; set => SetStyledField(value); }

        [Styled(nameof(_backgroundColor))] public UIColor BackgroundColor { get => _backgroundColor; set => SetStyledField(value); }
        [Styled(nameof(_cornerRadius))] public Vector4 CornerRadius { get => _cornerRadius; set => SetStyledField(value); }

        [Styled(nameof(_strokeColor))] public UIColor StrokeColor { get => _strokeColor; set => SetStyledField(value); }
        [Styled(nameof(_strokePosition))] public StrokePosition StrokePosition { get => _strokePosition; set => SetStyledField(value); }
        [Styled(nameof(_strokeWidth))] public ushort StrokeWidth { get => _strokeWidth; set => SetStyledField(value); }
        #endregion
    }

    public readonly record struct LayoutBehaviour(bool AsGroup)
    {
        public static LayoutBehaviour Default => new LayoutBehaviour(false);
    }

    public delegate void WidgetEventHandler(Widget widget);
    public delegate void WidgetEventHandler<T0>(Widget widget, T0 arg0);
    public delegate void WidgetEventHandler<T0, T1>(Widget widget, T0 arg0, T1 arg1);
    public delegate void WidgetEventHandler<T0, T1, T2>(Widget widget, T0 arg0, T1 arg1, T2 arg2);
    public delegate void WidgetEventHandler<T0, T1, T2, T3>(Widget widget, T0 arg0, T1 arg1, T2 arg2, T3 arg3);

    public enum WidgetInputState : byte
    {
        Sink = 0,       // sink inputs into this or children
        Passthrough,    // don't allow this to have inputs but still allow children
        Never,          // this nor it's children will get events
        Swallow         // consume any inputs on without considering any children
    }

    public enum AutoResizeMode : byte
    {
        None = 0,
        ResizeX,
        ResizeY,
        ResizeXY
    }
}
