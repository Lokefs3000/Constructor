using System.Numerics;
using EditorUI.Common;
using EditorUI.Layout;
using EditorUI.Mathematics;
using EditorUI.Styling;
using EditorUI.Visual;
using Primary;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.Utility;

namespace EditorUI.Widgets
{
    public class Widget : StyledObject
    {
        protected bool _isDestroyed;

        protected string? _id;

        protected bool _isEnabled;
        protected WidgetInputState _inputState;

        protected Widget? _parent;
        protected List<Widget>? _children;

        protected UIValue2 _position;
        protected UIValue2 _size;

        protected Vector2 _anchor;

        protected UIColor _backgroundColor;

        // internal state
        protected Vector2 _idealPosition;
        protected Vector2 _idealSize;
        protected Boundaries _computedRect;

        protected StateFlags _stateFlags;

        public Widget()
        {
            _id = null;

            _isEnabled = true;
            _inputState = WidgetInputState.Sink;

            _parent = null;
            _children = null;

            _position = UIValue2.Zero;
            _size = UIValue2.Zero;

            _anchor = Vector2.Zero;

            _backgroundColor = Color.White;

            _idealPosition = Vector2.Zero;
            _idealSize = Vector2.Zero;
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
            ChangeActiveParent(null);
            _isDestroyed = true;
        }

        protected internal virtual void MeasureSelf(ref readonly LayoutContext context)
        {
            _idealSize = _size.Evaluate(context.ParentSize);
        }

        protected internal virtual ChildrenLayoutMode LayoutSelf(ref readonly LayoutContext context)
        {
            Vector2 offset = context.IsLayoutLocked ? _idealPosition : _position.Evaluate(context.ParentSize);

            if (!_anchor.Equals(Vector2.Zero))
                offset -= _anchor * _idealSize;

            _computedRect = new Boundaries(offset, offset + _idealSize);
            return ChildrenLayoutMode.None;
        }

        protected internal virtual void PaintSelf(ref readonly PainterContext context)
        {
            if (_backgroundColor.IsVisible)
            {
                context.AddRectangle(_computedRect, new Paint(_backgroundColor));
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

                AddStateFlags(StateFlags.SelfInvalidLayout);
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
        #endregion

        #region State
        public override void AddStateFlags(StateFlags flags)
        {
            _stateFlags |= flags;

            // ripple upwards
            if (_parent != null)
            {
                flags &= ~StateFlags.This;
                if (flags != StateFlags.None && !_parent._stateFlags.HasAny(flags))
                {
                    _parent.AddStateFlags(flags);
                }
            }
        }

        public void RemoveStateFlags(StateFlags flags)
        {
            _stateFlags &= ~flags;
        }
        #endregion

        public bool IsDestroyed => _isDestroyed;

        public Widget? Parent { get => _parent; set => ChangeActiveParent(value); }
        public ROList<Widget> Children => _children ?? ROList<Widget>.Empty;

        public Vector2 IdealSize => _idealSize;
        public Vector2 IdealPosition => _idealPosition;
        public Boundaries ComputedRect => _computedRect;

        public StateFlags StateFlags => _stateFlags;

        #region StyledObject
        protected internal override StyledObject? ParentObject => _parent;
        #endregion
        #region Serializable
        [Styled(nameof(_id), isEditable: true)] public string? Id { get => _id; set => SetEditedField(value); }

        [Styled(nameof(_isEnabled), StateFlags.SelfInvalidLayout, true, true)] public bool IsEnabled { get => _isEnabled; set => SetEditedField(value); }
        [Styled(nameof(_inputState), isEditable: true)] public WidgetInputState InputState { get => _inputState; set => SetEditedField(value); }

        [Styled(nameof(_position), StateFlags.SelfInvalidLayout)] public UIValue2 Position { get => _position; set => SetStyledField(value); }
        [Styled(nameof(_size), StateFlags.SelfInvalidLayout)] public UIValue2 Size { get => _size; set => SetStyledField(value); }

        [Styled(nameof(_anchor), StateFlags.SelfInvalidLayout)] public Vector2 Anchor { get => _anchor; set => SetStyledField(value); }

        [Styled(nameof(_backgroundColor))] public UIColor BackgroundColor { get => _backgroundColor; set => SetStyledField(value); }
        #endregion
    }

    public enum WidgetInputState : byte
    {
        Sink = 0,       // sink inputs into this or children
        Passthrough,    // don't allow this to have inputs but still allow children
        Never           // this nor it's children will get events
    }
}
