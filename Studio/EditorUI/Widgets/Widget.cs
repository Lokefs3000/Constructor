using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
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
using Primary.Extensions;
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

        // Internal state
        protected WidgetLayoutState _layoutState;
        protected LayoutLockAxis _positionLockAxis;
        protected LayoutLockAxis _sizeLockAxis;

        // This widgets rect in the window
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

            _layoutState = WidgetLayoutState.Zero;
            _positionLockAxis = LayoutLockAxis.None;
            _sizeLockAxis = LayoutLockAxis.None;

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

        protected internal virtual MeasureStatus MeasureSelf(ref readonly LayoutContext context)
        {
            if (!context.LayoutLock.HasFlags(LayoutLockAxis.AxisX))
            {
                if (_autoResize == AutoResizeMode.ResizeX || _autoResize == AutoResizeMode.ResizeXY)
                    _layoutState.IdealSize.X = float.NegativeZero;
                else
                    _layoutState.IdealSize.X = _size.X.Evaluate(context.ParentSize.X);
            }

            if (!context.LayoutLock.HasFlags(LayoutLockAxis.AxisY))
            {
                if (_autoResize == AutoResizeMode.ResizeY || _autoResize == AutoResizeMode.ResizeXY)
                    _layoutState.IdealSize.Y = float.NegativeZero;
                else
                    _layoutState.IdealSize.Y = _size.Y.Evaluate(context.ParentSize.Y);
            }

            _layoutState.ContentSize = _layoutState.IdealSize;
            if (Vector4.GreaterThanAny(_padding, Vector4.Zero))
                _layoutState.ContentSize -= _padding.GetLower() + _padding.GetUpper();

            return MeasureStatus.Success;
        }

        protected internal virtual LayoutReturnData LayoutSelf(ref readonly LayoutContext context)
        {
            bool hasAnyMargin = Vector4.GreaterThanAny(_margin, Vector4.Zero);

            if (!context.LayoutLock.HasFlags(LayoutLockAxis.AxisX))
            {
                _layoutState.IdealPosition.X = _position.X.Evaluate(context.ParentSize.X);
                if (hasAnyMargin)
                    _layoutState.IdealPosition.X += _margin.X;
            }

            if (!context.LayoutLock.HasFlags(LayoutLockAxis.AxisY))
            {
                _layoutState.IdealPosition.Y = _position.Y.Evaluate(context.ParentSize.Y);
                if (hasAnyMargin)
                    _layoutState.IdealPosition.X += _margin.X;
            }

            if (!_anchor.Equals(Vector2.Zero))
                _layoutState.IdealPosition -= _anchor * _layoutState.IdealSize;

            _layoutState.ContentPosition = _layoutState.IdealPosition;
            if (_padding.X > 0.0f || _padding.Y > 0.0f)
            {
                _layoutState.ContentPosition.X += _padding.X;
                _layoutState.ContentPosition.Y += _padding.Y;
            }

            return LayoutReturnData.Success;
        }

        protected internal virtual void AfterComputedRectSelf()
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

        public virtual bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
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

            return false;
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
                UIManager.Instance.InputManager.ForceInputUpdate();
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
        [StyleUpdateCallback(nameof(InputState))]
        private void OnWidgetInputStateChanged()
        {
            if (!_isEnabled)
                return;

            switch (_inputState)
            {
                case WidgetInputState.Sink: UIManager.Instance.InputManager.ForceInputUpdate(); break;
                case WidgetInputState.Passthrough: UIManager.Instance.InputManager.ForgetInteractable(this); break;
                case WidgetInputState.Never: UIManager.Instance.InputManager.ForgetInteractable(this); break;
                case WidgetInputState.Swallow: UIManager.Instance.InputManager.ForceInputUpdate(); break;
            }
        }

        [StyleUpdateCallback(nameof(IsEnabled))]
        private void OnWidgetEnableStateChanged()
        {
            if (_isEnabled)
                UIManager.Instance.InputManager.ForceInputUpdate();
            else
                UIManager.Instance.InputManager.ForgetInteractable(this);
        }

        public virtual IInteractable GetInteractable(Vector2 point) => this;
        #endregion

        public virtual LayoutBehaviour LayoutBehaviour => LayoutBehaviour.Default;

        public bool IsDestroyed => _isDestroyed;

        public Widget? Parent { get => _parent; set => ChangeActiveParent(value); }
        public ROList<Widget> Children => _children ?? ROList<Widget>.Empty;
        public ROList<Stylist> Stylists => _stylists ?? ROList<Stylist>.Empty;

        public Vector2 IdealSize { get => _layoutState.IdealSize; protected internal set => _layoutState.IdealSize = value; }
        public Vector2 IdealPosition { get => _layoutState.IdealPosition; protected internal set => _layoutState.IdealPosition = value; }

        public Vector2 ContentPosition { get => _layoutState.ContentPosition; protected internal set => _layoutState.ContentPosition = value; }
        public Vector2 ContentSize { get => _layoutState.ContentSize; protected internal set => _layoutState.ContentSize = value; }

        public LayoutLockAxis PositionLockAxis { get => _positionLockAxis; protected internal set => _positionLockAxis = value; }
        public LayoutLockAxis SizeLockAxis { get => _sizeLockAxis; protected internal set => _sizeLockAxis = value; }

        public Boundaries ComputedRect { get => _computedRect; protected internal set => _computedRect = value; }

        internal ref WidgetLayoutState LayoutState => ref _layoutState;

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

        [Styled(nameof(_isEnabled), StateFlags.SelfInvalidLayout, true, true)]
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                    SetEditedField(value);
            }
        }
        [Styled(nameof(_inputState), isEditable: true)]
        public WidgetInputState InputState
        {
            get => _inputState;
            set
            {
                if (_inputState != value)
                    SetEditedField(value);
            }
        }

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

    [StructLayout(LayoutKind.Explicit)]
    public record struct WidgetLayoutState
    {
        [FieldOffset(0)] internal Vector256<float> Vector;

        [FieldOffset(sizeof(float) * 0)] internal Vector128<float> Position;
        [FieldOffset(sizeof(float) * 4)] internal Vector128<float> Size;

        [FieldOffset(sizeof(float) * 0)] public Vector2 IdealPosition;
        [FieldOffset(sizeof(float) * 2)] public Vector2 ContentPosition;

        [FieldOffset(sizeof(float) * 4)] public Vector2 IdealSize;
        [FieldOffset(sizeof(float) * 6)] public Vector2 ContentSize;

        public static WidgetLayoutState Zero => Vector256<float>.Zero;

        public static implicit operator WidgetLayoutState(Vector256<float> vector) => Unsafe.BitCast<Vector256<float>, WidgetLayoutState>(vector);
        public static implicit operator Vector256<float>(WidgetLayoutState layoutState) => Unsafe.BitCast<WidgetLayoutState, Vector256<float>>(layoutState);
    }

    [StructLayout(LayoutKind.Explicit)]
    public record struct LayoutState
    {
        
    }

    public record struct LayoutGridState
    {
        
    }

    public readonly record struct LayoutBehaviour(bool AsGroup, bool ListenToChildren)
    {
        public LayoutBehaviour(LayoutBehaviour template) : this(template.AsGroup, template.ListenToChildren)
        {
        }

        public static LayoutBehaviour Default => new LayoutBehaviour(false, false);
        public static LayoutBehaviour Group => new LayoutBehaviour(true, false);
    }

    public delegate void WidgetEventHandler(Widget widget);
    public delegate void WidgetEventHandler<T0>(Widget widget, T0 arg0);
    public delegate void WidgetEventHandler<T0, T1>(Widget widget, T0 arg0, T1 arg1);
    public delegate void WidgetEventHandler<T0, T1, T2>(Widget widget, T0 arg0, T1 arg1, T2 arg2);
    public delegate void WidgetEventHandler<T0, T1, T2, T3>(Widget widget, T0 arg0, T1 arg1, T2 arg2, T3 arg3);

    public enum WidgetInputState : byte
    {
        /// <summary>Sink inputs into this or children</summary>
        Sink = 0,
        /// <summary>Don't allow this to have inputs but still allow children</summary>
        Passthrough,
        /// <summary>Neither this nor it's children will get events</summary>
        Never,
        /// <summary>Consume any inputs on without considering any children</summary>
        Swallow,
        /// <summary>Intercept any input that has not been consumed by a descendent</summary>
        /// <remarks>Certain interactable specific events will not be propagated since it would result in a messed up state.<br/>fx. 2 interactables cannot both recieve a <see cref="UIInputEventType.MouseEnter"/> since only one can be under the mouse at any given time</remarks>
        Intercept
    }

    public enum AutoResizeMode : byte
    {
        None = 0,
        ResizeX,
        ResizeY,
        ResizeXY
    }

    public enum OverflowMode : byte
    {
        Visible = 0,
        Hidden,
        Clip,
        Scroll,
    }

    public enum ItemAlignment : byte
    {
        Center = 0,
        Start,
        End,
        Baseline,
        Stretch,
    }

    // Display data setup
    //   Flow: 00000000000-bb-a
    //   Flex: 0-hhhh-gg-ff-ee-bb-a
    //   Grid: 000000000-d-c-bb-a

    public enum DisplayOutside : ushort
    {
        /// <summary>Add flow break if there isn't enough space for this widget</summary>
        Inline = 0b0,

        /// <summary>Add flow break before and after this widget</summary>
        Block = 0b1,

        // Metadata

        Shift = 0,
        Mask = 0b1 << Shift,
    }

    public enum DisplayInside : ushort
    {
        /// <summary>Layout widgets after each other depending on their <see cref="Widget.DisplayOutside"/> property</summary>
        Flow = 0b000,

        /// <summary>Layout widgets according to the current flexbox setup</summary>
        Flex = 0b010,

        /// <summary>Layout widgets using a grid</summary>
        Grid = 0b100,

        // Metadata

        Shift = 1,
        Mask = 0b11 << Shift
    }

    public enum GridColumnsMethod : ushort
    {
        Automatic = 0b0,
        Templated = 0b1,

        // Metadata

        RowShift = 3,
        ColumnShift = 4,

        RowMask = 0b1 << RowShift,
        ColumnMask = 0b1 << ColumnShift
    }

    public enum FlexDirection : ushort
    {
        Row = 0b00,
        RowReverse = 0b01,
        Column = 0b10,
        ColumnReverse = 0b11,

        // Metadata

        Shift = 3,
        Mask = 0b11 << Shift
    }

    public enum FlexWrapMode : ushort
    {
        Dont = 0b00,
        Wrap = 0b01,
        WrapReverse = 0b10,

        // Metadata

        Shift = 5,
        Mask = 0b11 << Shift
    }

    public enum FlexItemAlignment : ushort
    {
        Start           = 0b0000,
        End             = 0b0001,
        Left            = 0b0010,
        Right           = 0b0011,
        Center          = 0b0100,
        SpaceAround     = 0b0101,
        SpaceBetween    = 0b0110,
        SpaceEvenly     = 0b0111,
        Stretch         = 0b1000,

        // Metadata

        Shift = 7,
        Mask = 0b1111 << Shift
    }
}
