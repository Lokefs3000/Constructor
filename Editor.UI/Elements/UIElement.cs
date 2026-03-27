using Editor.UI.Datatypes;
using Editor.UI.Debugging;
using Editor.UI.Interaction;
using Editor.UI.Layout;
using Editor.UI.Modifiers;
using Editor.UI.Reflection;
using Editor.UI.Styling;
using Editor.UI.Visual;
using Primary.Common;
using Primary.GUI.ImGui;
using Primary.Input.Devices;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("Element")]
    public class UIElement : StyleBase
    {
        private UIWindow? _windowOwner;

        private UIElement? _parent;
        private List<UIElement> _children;

        private List<IUILayoutModifier>? _layoutMods;

        private UIStateFlags _stateFlags;

        private Boundaries _elementTreeBounds;

        protected Vector2 _relativeOffset;
        protected Vector2 _currentSize;

        protected Boundaries _pixelCoordinates;

        protected string? _id;
        protected bool _isActive;
        protected bool _isEnabled;

        protected UIValue2 _position;
        protected UIValue2 _size;
        protected Vector2 _anchor;

        public UIElement()
        {
            _windowOwner = null;

            _parent = null;
            _children = new List<UIElement>();

            _layoutMods = null;

            _stateFlags = UIStateFlags.InvalidAll;

            _id = null;
            _isActive = true;
            _isEnabled = true;

            _elementTreeBounds = Boundaries.Zero;

            _position = UIValue2.Zero;
            _size = UIValue2.Zero;
            _anchor = Vector2.Zero;

            _relativeOffset = Vector2.Zero;
            _currentSize = Vector2.Zero;

            _pixelCoordinates = Boundaries.Zero;
        }

        public UIElement(UIElement parent) : this()
        {
            SetParent(parent);
        }

        public void Destroy()
        {
            DestroySelf();

            OnDestroy?.Invoke();

            _parent?._children.Remove(this);

            _windowOwner?.StyleUpdater.RemoveInvalidStyleBase(this);
            _windowOwner?.ParentHost?.InteractionManager?.DeferenceDestroyedElement(this);

            _windowOwner = null;
            _parent = null;
        }

        protected virtual void DestroySelf() { }

        internal void SetParent(UIElement newParent)
        {
            if (_parent == newParent || newParent == this)
                return;

            _parent?._children.Remove(this);
            newParent._children.Add(this);

            _parent = newParent;

            _parent?.AddStateFlags(UIStateFlags.InvalidAll);
            newParent.AddStateFlags(UIStateFlags.InvalidAll);

            SetNewAndUpdateChildren(newParent._windowOwner);
            AddStateFlags(UIStateFlags.InvalidLayout);
        }

        /// <summary>Same as doing: <c>element.Parent = this</c></summary>
        public void AddChild(UIElement element)
        {
            if (element._parent != this)
                element.SetParent(this);
        }

        public void MoveChild(UIElement child, int newIndex)
        {
            if (_children.IndexOf(child) != newIndex && _children.Remove(child))
            {
                _children.Insert(newIndex, child);
                AddStateFlags(UIStateFlags.InvalidLayout);
            }
        }

        public void ClearChildren()
        {
            while (_children.Count > 0)
            {
                _children[0].Destroy();
            }
        }

        internal void SetNewAndUpdateChildren(UIWindow? newOwner)
        {
            if (_windowOwner != newOwner)
            {
                InvalidateAll();

                if (HasInvalidProperties)
                {
                    _windowOwner?.StyleUpdater.RemoveInvalidStyleBase(this);
                    newOwner?.StyleUpdater.AddInvalidStyleBase(this);
                }

                _windowOwner = newOwner;

                if (_children.Count > 0)
                {
                    foreach (UIElement child in _children)
                    {
                        if (child._windowOwner != newOwner)
                        {
                            child.SetNewAndUpdateChildren(newOwner);
                        }
                    }
                }
            }
        }

        public T AddLayoutModifier<T>() where T : class, IUILayoutModifier
        {
            T mod = (T)Activator.CreateInstance(typeof(T), [this])!;
            (_layoutMods ??= new List<IUILayoutModifier>()).Add(mod);

            return mod;
        }

        public IUILayoutModifier AddLayoutModifier(Type type)
        {
            IUILayoutModifier mod = (IUILayoutModifier)Activator.CreateInstance(type, [this])!;
            (_layoutMods ??= new List<IUILayoutModifier>()).Add(mod);

            return mod;
        }

        public T? RemoveLayoutModifier<T>(T? target = null) where T : class, IUILayoutModifier
        {
            if (_layoutMods != null)
            {
                if (target == null)
                {
                    _layoutMods.RemoveWhere((x) => x is T, out IUILayoutModifier? mod);
                    return Unsafe.As<T>(mod);
                }
                else
                {
                    return _layoutMods.Remove(target) ? target : null;
                }
            }
            else
                return null;
        }

        public IUILayoutModifier? RemoveLayoutModifier(Type type, IUILayoutModifier? target = null)
        {
            if (_layoutMods != null)
            {
                if (target == null)
                {
                    _layoutMods.RemoveWhere((x) => x.GetType() == type, out IUILayoutModifier? mod);
                    return mod;
                }
                else
                {
                    return _layoutMods.Remove(target) ? target : null;
                }
            }
            else
                return null;
        }

        public void AddStateFlags(UIStateFlags flags)
        {
            _stateFlags |= flags;

            if (_parent != null)
            {
                flags &= UIStateFlags.InvalidAll;

                if (flags > UIStateFlags.None)
                {
                    _parent?.AddStateFlags(flags);

                    foreach (UIElement child in _children)
                        child.AddStateFlagsDescending(flags);
                }
            }
            else if (_windowOwner != null)
            {
                _windowOwner.ParentHost?.AddStateFlags(_stateFlags);
            }
        }

        private void AddStateFlagsDescending(UIStateFlags flags)
        {
            _stateFlags |= flags;

            foreach (UIElement child in _children)
                child.AddStateFlagsDescending(flags);
        }

        internal void RemoveStateFlags(UIStateFlags flags)
        {
            _stateFlags &= ~flags;
        }

        internal void ClearPreviousLayoutData()
        {
            _relativeOffset = Vector2.Zero;
            _currentSize = Vector2.Zero;
        }

        public virtual void MeasureSize(UIMeasureContext context)
        {
            Debug.Assert(_windowOwner != null);

            _currentSize = _size.Evaluate(context.LocalRegion);
        }

        public virtual void RecalculateLayout(UILayoutContext context)
        {
            _relativeOffset = _position.Evaluate(context.LocalRegion);

            if (!_anchor.Equals(Vector2.Zero))
                _relativeOffset -= _anchor * _currentSize;
        }

        public virtual bool DrawVisual(UIPainterContext painter)
        {
            return true;
        }

        public virtual void HandleEvent(HostInteractionManager interaction, ref readonly UIEvent @event)
        {

        }

        #region Modifiers
        internal void ExecuteMeasureMods(UIMeasureContext context)
        {
            if (_layoutMods != null)
            {
                foreach (IUILayoutModifier mod in _layoutMods)
                {
                    mod.MeasureSize(context);
                }
            }
        }

        internal void ExecuteLayoutMods(UILayoutContext context)
        {
            if (_layoutMods != null)
            {
                foreach (IUILayoutModifier mod in _layoutMods)
                {
                    context.Measurements.RefreshIfRequired();
                    mod.ModifyElement(context);
                }
            }
        }
        #endregion
        #region Event invokers
        [MethodImpl(MethodImplOptions.AggressiveInlining), StackTraceHidden]
        internal void Invoke_OnMouseMove(Vector2 position) => OnMouseMove?.Invoke(position);
        [MethodImpl(MethodImplOptions.AggressiveInlining), StackTraceHidden]
        internal void Invoke_OnMousePress(MouseButton button) => OnMousePress?.Invoke(button);
        [MethodImpl(MethodImplOptions.AggressiveInlining), StackTraceHidden]
        internal void Invoke_OnMouseRelease(MouseButton button) => OnMouseRelease?.Invoke(button);
        [MethodImpl(MethodImplOptions.AggressiveInlining), StackTraceHidden]
        internal void Invoke_OnDragStart(OnDragStartContext context) => OnDragStart?.Invoke(context);
        #endregion

        public UIWindow? WindowOwner => _windowOwner;

        public UIElement Parent { get => _parent!; set => SetParent(value); }
        public IReadOnlyList<UIElement> Children => _children;

        public IReadOnlyList<IUILayoutModifier>? LayoutModifiers => _layoutMods;

        public UIStateFlags StateFlags => _stateFlags;

        public Boundaries ElementTreeBounds { get => _elementTreeBounds; internal set => _elementTreeBounds = value; }

        public Vector2 RelativeOffset { get => _relativeOffset; set => _relativeOffset = value; }
        public Vector2 CurrentSize { get => _currentSize; set => _currentSize = value; }

        public Boundaries PixelCoordinates { get => _pixelCoordinates; internal set => _pixelCoordinates = value; }

        #region Styleable
        [EditableProperty(nameof(_id))] public string? Id { get => _id; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_isActive))] public bool IsActive { get => _isActive; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_isEnabled), UIStateFlags.InvalidAll)] public bool IsEnabled { get => _isEnabled; set => SetEditableProperty(value); }

        [StyleableProperty(nameof(_position), UIStateFlags.InvalidLayout)] public UIValue2 Position { get => _position; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_size), UIStateFlags.InvalidLayout)] public UIValue2 Size { get => _size; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_anchor), UIStateFlags.InvalidLayout)] public Vector2 Anchor { get => _anchor; set => SetStyleProperty(value); }
        #endregion
        #region Events
        public event Action? OnDestroy;

        public event Action<Vector2>? OnMouseMove;

        public event Action<MouseButton>? OnMousePress;
        public event Action<MouseButton>? OnMouseRelease;

        public event Action<OnDragStartContext>? OnDragStart;
        #endregion
    }
}
