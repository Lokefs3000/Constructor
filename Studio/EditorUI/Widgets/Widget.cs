using System.Collections;
using System.Collections.Specialized;
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

        [StyleSetup(IsEditable = true)] protected string? _id;

        [StyleSetup(StateFlags.SelfInvalidLayout, IsEditable = true)] protected bool _isEnabled;
        [StyleSetup(IsEditable = true)] protected WidgetInputState _inputState;

        protected Widget? _parent;
        protected List<Widget>? _children;
        protected List<Stylist>? _stylists;

        #region Layout
        [StyleSetup(StateFlags.SelfInvalidLayout)] protected PositionMode _position;
        protected OverflowMode _overflow;
        [StyleSetup(StateFlags.SelfInvalidLayout)] protected ItemAlignment _alignItems;

        [StyleSetup(StateFlags.SelfInvalidLayout)] protected UIValue? _left;
        [StyleSetup(StateFlags.SelfInvalidLayout)] protected UIValue? _right;

        [StyleSetup(StateFlags.SelfInvalidLayout)] protected UIValue? _top;
        [StyleSetup(StateFlags.SelfInvalidLayout)] protected UIValue? _bottom;

        [StyleSetup(StateFlags.SelfInvalidLayout)] protected UIValue? _width;
        [StyleSetup(StateFlags.SelfInvalidLayout)] protected UIValue? _height;

        [StyleSetup(StateFlags.SelfInvalidLayout, IsGroup = true)] protected LayoutVector2 _anchor;

        [StyleSetup(StateFlags.SelfInvalidLayout, IsGroup = true)] protected LayoutBox _margin;
        [StyleSetup(StateFlags.SelfInvalidLayout, IsGroup = true)] protected LayoutBox _padding;

        [StyleSetup(StateFlags.SelfInvalidLayout, IsGroup = true, Flatten = true)] protected DisplayState _display;

        [StyleSetup(StateFlags.SelfInvalidLayout)] protected float? _aspectRatio;
        #endregion

        #region Display
        [StyleInclude] protected UIColor _backgroundColor;
        [StyleInclude] protected Vector4 _cornerRadius;

        [StyleInclude] protected UIColor _strokeColor;
        [StyleInclude] protected StrokePosition _strokePosition;
        [StyleInclude] protected ushort _strokeWidth;
        #endregion

        // Internal state
        protected WidgetLayoutState _layoutState;
        protected LayoutLockAxis _positionLockAxis;
        protected LayoutLockAxis _sizeLockAxis;

        // This widgets rect in the window
        protected Boundaries _computedRect;

        protected LayoutChangeMask _changeMask;

        protected StateFlags _stateFlags;

        public Widget()
        {
            _id = null;

            _isEnabled = true;
            _inputState = WidgetInputState.Sink;

            _parent = null;
            _children = null;
            _stylists = null;

            _position = PositionMode.Relative;
            _overflow = OverflowMode.Visible;
            _alignItems = ItemAlignment.Start;

            _left = null;
            _right = null;

            _width = null;
            _height = null;

            _anchor = LayoutVector2.Null;

            _margin = LayoutBox.Null;
            _padding = LayoutBox.Null;

            _display = new DisplayState { DisplayOutside = DisplayOutside.Inline, DisplayInside = DisplayInside.Flow };

            _aspectRatio = null;

            _backgroundColor = Color.TransparentBlack;
            _cornerRadius = Vector4.Zero;

            _strokeColor = Color.TransparentBlack;
            _strokePosition = StrokePosition.Outside;
            _strokeWidth = 0;

            _layoutState = WidgetLayoutState.Zero;
            _positionLockAxis = LayoutLockAxis.None;
            _sizeLockAxis = LayoutLockAxis.None;

            _computedRect = Boundaries.Zero;

            _changeMask = LayoutChangeMask.None;

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

        protected internal virtual Vector2? QueryMeasurements(ref readonly LayoutContext context) => null;

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

        #region Layout callbacks
        [StyleUpdateCallback(nameof(Left), nameof(Right), nameof(Width), nameof(Height),
            nameof(MarginLeft), nameof(MarginTop), nameof(MarginRight), nameof(MarginBottom), nameof(AspectRatio))]
        private void OnTransformTypeChanged() => _changeMask |= LayoutChangeMask.Transform;

        [StyleUpdateCallback(nameof(AnchorX), nameof(AnchorY))]
        private void OnAnchorTypeChanged() => _changeMask |= LayoutChangeMask.Anchor;

        [StyleUpdateCallback(nameof(PaddingLeft), nameof(PaddingTop), nameof(PaddingRight), nameof(PaddingBottom), nameof(AlignItems),
            nameof(FlexAlignItems), nameof(FlexDirection), nameof(FlexWrap),
            nameof(GridRowAutoSize), nameof(GridRowTemplate), nameof(GridColumnAutoSize), nameof(GridColumnTemplate))]
        private void OnChildLayoutTypeChanged() => _changeMask |= LayoutChangeMask.ChildLayout;

        [StyleUpdateCallback(nameof(DisplayInside), nameof(DisplayOutside))]
        private void OnDisplayTypeChanged() => _changeMask |= LayoutChangeMask.Display;

        [StyleUpdateCallback(nameof(OverflowX), nameof(OverflowY))]
        private void OnOverflowTypeChanged() => _changeMask |= LayoutChangeMask.Overflow;

        [StyleUpdateCallback(nameof(Position))]
        private void OnPositionTypeChanged() => _changeMask |= LayoutChangeMask.Position;
        #endregion

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
        internal ref DisplayState DisplayState => ref _display;

        internal ref LayoutChangeMask ChangeMask => ref _changeMask;

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
        public string? Id { get => _id; set => SetEditedField(value); }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                    SetEditedField(value);
            }
        }

        public WidgetInputState InputState
        {
            get => _inputState;
            set
            {
                if (_inputState != value)
                    SetEditedField(value);
            }
        }

        public PositionMode Position { get => _position; set => SetStyledField(value); }
        [StyleSetup(StateFlags.SelfInvalidLayout)] public OverflowMode OverflowX { get => (OverflowMode)(((int)_overflow >> (int)OverflowMode.XShift) & (int)OverflowMode.XMask); set => SetStyledField((OverflowMode)(((int)_overflow << (int)OverflowMode.XShift) & (int)OverflowMode.XMask)); }
        [StyleSetup(StateFlags.SelfInvalidLayout)] public OverflowMode OverflowY { get => (OverflowMode)(((int)_overflow >> (int)OverflowMode.YShift) & (int)OverflowMode.XMask); set => SetStyledField((OverflowMode)(((int)_overflow << (int)OverflowMode.YShift) & (int)OverflowMode.YMask)); }
        public ItemAlignment AlignItems { get => _alignItems; set => SetStyledField(value); }

        public UIValue? Left { get => _left; set => SetStyledField(value); }
        public UIValue? Right { get => _right; set => SetStyledField(value); }

        public UIValue? Top { get => _top; set => SetStyledField(value); }
        public UIValue? Bottom { get => _bottom; set => SetStyledField(value); }

        public UIValue? Width { get => _width; set => SetStyledField(value); }
        public UIValue? Height { get => _height; set => SetStyledField(value); }

        public LayoutVector2 Anchor
        {
            get => _anchor;
            set
            {
                SetStyledField(value.X, nameof(AnchorX));
                SetStyledField(value.Y, nameof(AnchorY));
            }
        }

        public LayoutBox Margin
        {
            get => _margin;
            set
            {
                SetStyledField(value.Left, nameof(MarginLeft));
                SetStyledField(value.Top, nameof(MarginTop));
                SetStyledField(value.Right, nameof(MarginRight));
                SetStyledField(value.Bottom, nameof(MarginBottom));
            }
        }

        public LayoutBox Padding
        {
            get => _padding;
            set
            {
                SetStyledField(value.Left, nameof(PaddingLeft));
                SetStyledField(value.Top, nameof(PaddingTop));
                SetStyledField(value.Right, nameof(PaddingRight));
                SetStyledField(value.Bottom, nameof(PaddingBottom));
            }
        }

        public DisplayOutside DisplayOutside { get => _display.DisplayOutside; set => SetStyledField(value); }
        public DisplayInside DisplayInside { get => _display.DisplayInside; set => SetStyledField(value); }

        public UIValue? GridRowTemplate { get => _display.Grid.RowTemplate; set => SetStyledField(value); }
        public int? GridRowAutoSize { get => _display.Grid.RowAutoSize; set => SetStyledField(value); }

        public UIValue? GridColumnTemplate { get => _display.Grid.ColumnTemplate; set => SetStyledField(value); }
        public int? GridColumnAutoSize { get => _display.Grid.ColumnAutoSize; set => SetStyledField(value); }

        public FlexDirection FlexDirection { get => _display.Flex.Direction; set => SetStyledField(value); }
        public FlexWrapMode FlexWrap { get => _display.Flex.WrapMode; set => SetStyledField(value); }
        public FlexItemAlignment FlexAlignItems { get => _display.Flex.AlignItems; set => SetStyledField(value); }

        public float? AspectRatio { get => _aspectRatio; set => SetStyledField(value); }

        public UIColor BackgroundColor { get => _backgroundColor; set => SetStyledField(value); }
        public Vector4 CornerRadius { get => _cornerRadius; set => SetStyledField(value); }

        public UIColor StrokeColor { get => _strokeColor; set => SetStyledField(value); }
        public StrokePosition StrokePosition { get => _strokePosition; set => SetStyledField(value); }
        public ushort StrokeWidth { get => _strokeWidth; set => SetStyledField(value); }

        #region Extended
        public float? AnchorX { get => _anchor.X; set => SetStyledField(value); }
        public float? AnchorY { get => _anchor.Y; set => SetStyledField(value); }

        public int? MarginLeft { get => _margin.Left; set => SetStyledField(value); }
        public int? MarginTop { get => _margin.Top; set => SetStyledField(value); }
        public int? MarginRight { get => _margin.Right; set => SetStyledField(value); }
        public int? MarginBottom { get => _margin.Bottom; set => SetStyledField(value); }

        public int? PaddingLeft { get => _padding.Left; set => SetStyledField(value); }
        public int? PaddingTop { get => _padding.Top; set => SetStyledField(value); }
        public int? PaddingRight { get => _padding.Right; set => SetStyledField(value); }
        public int? PaddingBottom { get => _padding.Bottom; set => SetStyledField(value); }

        public OverflowMode Overflow
        {
            get => _overflow;
            set
            {
                OverflowX = value;
                OverflowY = value;
            }
        }
        #endregion

        #endregion
    }

    public record struct LayoutVector2
    {
        [StyleInclude] public float? X;
        [StyleInclude] public float? Y;

        public static LayoutVector2 Null => new LayoutVector2 { X = null, Y = null };
    }

    public record struct LayoutBox
    {
        [StyleInclude] public int? Left;
        [StyleInclude] public int? Top;
        [StyleInclude] public int? Right;
        [StyleInclude] public int? Bottom;

        public LayoutBox(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public static LayoutBox Null => new LayoutBox { Left = null, Top = null, Right = null, Bottom = null };
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
    public record struct DisplayState
    {
        [FieldOffset(0)] public ushort Value;
        [FieldOffset(0), StyleSetup(Flatten = true), StyleCondition(nameof(DisplayInside), DisplayInside.Grid)] public GridDisplayState Grid;
        [FieldOffset(0), StyleSetup(Flatten = true), StyleCondition(nameof(DisplayInside), DisplayInside.Flex)] public FlexDisplayState Flex;

        [StyleInclude]
        public DisplayOutside DisplayOutside
        {
            readonly get => (DisplayOutside)((Value & (ushort)DisplayOutside.Mask) >> (ushort)DisplayOutside.Shift);
            set => Value = (ushort)((Value & ~(ushort)DisplayOutside.Mask) | (ushort)value);
        }

        [StyleInclude]
        public DisplayInside DisplayInside
        {
            readonly get => (DisplayInside)((Value & (ushort)DisplayInside.Mask) >> (ushort)DisplayInside.Shift);
            set => Value = (ushort)((Value & ~(ushort)DisplayInside.Mask) | (ushort)value);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public record struct GridDisplayState
    {
        [FieldOffset(0)] public ushort Value;

        [FieldOffset(2)] public UIValue? RowSize;
        [FieldOffset(10)] public UIValue? ColumnSize;

        public GridColumnsMethod Rows
        {
            readonly get => (GridColumnsMethod)((Value & (ushort)GridColumnsMethod.RowMask) >> (ushort)GridColumnsMethod.RowShift);
            set => Value = (ushort)((Value & ~(ushort)GridColumnsMethod.RowMask) | ((ushort)value << (ushort)GridColumnsMethod.RowShift));
        }

        public GridColumnsMethod Columns
        {
            readonly get => (GridColumnsMethod)((Value & (ushort)GridColumnsMethod.ColumnMask) >> (ushort)GridColumnsMethod.ColumnShift);
            set => Value = (ushort)((Value & ~(ushort)GridColumnsMethod.ColumnMask) | ((ushort)value << (ushort)GridColumnsMethod.ColumnShift));
        }

        [StyleInclude, StyleLink(nameof(Rows), GridColumnsMethod.Templated)] public UIValue? RowTemplate { get => RowSize; set => RowSize = value; }
        [StyleInclude, StyleLink(nameof(Rows), GridColumnsMethod.Automatic)] public int? RowAutoSize { get => RowSize?.Absolute; set => RowSize = value.HasValue ? new UIValue(value.Value) : null; }

        [StyleInclude, StyleLink(nameof(Columns), GridColumnsMethod.Templated)] public UIValue? ColumnTemplate { get => ColumnSize; set => ColumnSize = value; }
        [StyleInclude, StyleLink(nameof(Columns), GridColumnsMethod.Automatic)] public int? ColumnAutoSize { get => ColumnSize?.Absolute; set => ColumnSize = value.HasValue ? new UIValue(value.Value) : null; }
    }

    [StructLayout(LayoutKind.Explicit)]
    public record struct FlexDisplayState
    {
        [FieldOffset(0)] public ushort Value;

        [StyleInclude]
        public FlexDirection Direction
        {
            readonly get => (FlexDirection)((Value & (ushort)FlexDirection.Mask) >> (ushort)FlexDirection.Shift);
            set => Value = (ushort)((Value & ~(ushort)FlexDirection.Mask) | ((ushort)value << (ushort)FlexDirection.Shift));
        }

        [StyleSetup(AliasAs = "Wrap")]
        public FlexWrapMode WrapMode
        {
            readonly get => (FlexWrapMode)((Value & (ushort)FlexWrapMode.Mask) >> (ushort)FlexWrapMode.Shift);
            set => Value = (ushort)((Value & ~(ushort)FlexWrapMode.Mask) | ((ushort)value << (ushort)FlexWrapMode.Shift));
        }

        [StyleInclude]
        public FlexItemAlignment AlignItems
        {
            readonly get => (FlexItemAlignment)((Value & (ushort)FlexItemAlignment.Mask) >> (ushort)FlexItemAlignment.Shift);
            set => Value = (ushort)((Value & ~(ushort)FlexItemAlignment.Mask) | ((ushort)value << (ushort)FlexItemAlignment.Shift));
        }
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

    public enum LayoutChangeMask : byte
    {
        None = 0,

        Transform = 1 << 0,
        Display = 1 << 1,
        Overflow = 1 << 2,
        ChildLayout = 1 << 3,
        Anchor = 1 << 4,
        Position = 1 << 5
    }

    public enum PositionMode : byte
    {
        Relative = 0,
        Absolute
    }

    public enum OverflowMode : byte
    {
        Visible = 0,
        Hidden,
        Clip,
        Scroll,

        // Metadata
        XShift = 0,
        YShift = 4,

        XMask = 0b1111 << XShift,
        YMask = 0b1111 << YShift
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
        Start = 0b0000,
        End = 0b0001,
        Left = 0b0010,
        Right = 0b0011,
        Center = 0b0100,
        SpaceAround = 0b0101,
        SpaceBetween = 0b0110,
        SpaceEvenly = 0b0111,
        Stretch = 0b1000,

        // Metadata

        Shift = 7,
        Mask = 0b1111 << Shift
    }
}
