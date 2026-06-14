using Editor.UI.Datatypes;
using Editor.UI.Diagnostics;
using Editor.UI.Interaction;
using Editor.UI.Layout;
using Editor.UI.Modifiers;
using Editor.UI.Reflection;
using Editor.UI.Styling;
using Editor.UI.Visual;
using Primary.Common;
using Primary.GUI.ImGui;
using Primary.Input.Devices;
using Primary.Mathematics;
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
    public class UIElement : StyleBase, IInteractable
    {
        private IWindow? _windowOwner;

        private bool _isDestroyed;

        private UIElement? _parent;
        private List<UIElement> _children;

        private List<IUILayoutModifier>? _layoutMods;

        private UIStateFlags _stateFlags;

        private Boundaries _elementTreeBounds;
        protected Vector2 _childExtents;

        protected Vector2 _relativeOffset;
        protected Vector2 _currentSize;

        protected Vector2 _viewOffset;
        protected Vector2 _viewSize;

        protected Vector2 _scrollPosition;

        protected Boundaries _pixelCoordinates;
        protected Boundaries _viewCoordinates;

        protected string? _id;
        protected bool _isActive;
        protected bool _isEnabled;

        protected bool _clipDescendents;

        protected UIValue2 _position;
        protected UIValue2 _size;
        protected Vector2 _anchor;

        public UIElement()
        {
            _windowOwner = null;

            _isDestroyed = false;

            _parent = null;
            _children = new List<UIElement>();

            _layoutMods = null;

            _stateFlags = UIStateFlags.InvalidAll;

            _id = null;
            _isActive = true;
            _isEnabled = true;

            _elementTreeBounds = Boundaries.Zero;
            _childExtents = Vector2.Zero;

            _relativeOffset = Vector2.Zero;
            _currentSize = Vector2.Zero;

            _viewOffset = Vector2.Zero;
            _viewSize = Vector2.Zero;

            _scrollPosition = Vector2.Zero;

            _pixelCoordinates = Boundaries.Zero;
            _viewCoordinates = Boundaries.Zero;

            _position = UIValue2.Zero;
            _size = UIValue2.Zero;
            _anchor = Vector2.Zero;
        }

        public UIElement(UIElement parent) : this()
        {
            SetParent(parent);
        }

        public void Destroy()
        {
            if (_isDestroyed)
                return;
            _isDestroyed = true;

            DestroySelf();

            OnDestroy?.Invoke();

            _parent?._children.Remove(this);

            _windowOwner?.StyleUpdater.RemoveInvalidStyleBase(this);
            _windowOwner?.ParentHost?.InteractionManager?.DereferenceDestroyedInteractable(this);

            _windowOwner = null;
            _parent = null;

            while (_children.Count > 0)
            {
                _children[0].Destroy();
            }
        }

        protected virtual void DestroySelf() { }
        protected virtual void WindowChangingSelf(IElementOwner? previousWindow, bool isCascaded) { }
        protected virtual void ScrollPositionChanged() { }

        protected void SetParent(UIElement newParent)
        {
            if (_parent == newParent || newParent == this)
                return;

            UIElement? prevParent = _parent;

            _parent?._children.Remove(this);
            newParent._children.Add(this);

            _parent?.AddStateFlags(UIStateFlags.InvalidAll);
            newParent.AddStateFlags(UIStateFlags.InvalidAll);

            _parent = newParent;

            SetNewAndUpdateChildren(newParent._windowOwner);
            AddStateFlags(UIStateFlags.InvalidAll);
        }

        /// <summary>Same as doing: <c>element.Parent = null</c></summary>
        public void ClearParent()
        {
            if (_parent != null)
            {
                _parent?._children.Remove(this);
                _parent?.AddStateFlags(UIStateFlags.InvalidAll);

                _parent = null;

                SetNewAndUpdateChildren(null);
                AddStateFlags(UIStateFlags.InvalidAll);
            }
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

        internal void SetNewAndUpdateChildren(IWindow? newOwner, bool hasCascaded = false)
        {
            if (_windowOwner != newOwner)
            {
                if (HasInvalidProperties)
                {
                    _windowOwner?.StyleUpdater.RemoveInvalidStyleBase(this);
                    newOwner?.StyleUpdater.AddInvalidStyleBase(this);
                }

                IWindow? prev = _windowOwner;
                _windowOwner = newOwner;

                InvalidateAll();
                WindowChangingSelf(prev, hasCascaded);

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

        public virtual void ClearPreviousLayoutData()
        {
            _relativeOffset = Vector2.Zero;
            _currentSize = Vector2.Zero;

            _viewOffset = Vector2.Zero;
            _viewSize = Vector2.Zero;

            _childExtents = Vector2.Zero;
        }

        public virtual void MeasureSize(UIMeasureContext context)
        {
            Debug.Assert(_windowOwner != null);

            _currentSize = _size.Evaluate(context.LocalRegion);
            _viewSize = Vector2.Zero;
        }

        public virtual void RecalculateLayout(UILayoutContext context)
        {
            _relativeOffset = _position.Evaluate(context.LocalRegion);
            _viewOffset = Vector2.Zero;
        }

        public virtual void FinalizeLayout()
        {
            
        }

        public virtual bool DrawVisual(UIPainterContext painter)
        {
            return true;
        }

        public virtual void HandleEvent(ref readonly UIEvent @event)
        {

        }

        public virtual IInteractable GetInteractable(Vector2 point)
        {
            if (_layoutMods != null)
            {
                for (int i = _layoutMods.Count - 1; i >= 0; --i)
                {
                    IUILayoutModifier mod = _layoutMods[i];
                    if (mod is IInteractable interactable)
                    {
                        IInteractionShape? shape = interactable.Shape;
                        if (shape != null && shape.Intersects(point))
                            return interactable.GetInteractable(point);
                    }
                }
            }

            return this;
        }

        #region Modifiers
        internal void ExecuteMeasureMods(UIMeasureContext context)
        {
            if (_layoutMods != null)
            {
                foreach (IUILayoutModifier mod in _layoutMods)
                {
                    mod.ModifySize(context);
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
                    mod.ModifyLayout(context);
                }
            }
        }

        internal void ExecuteVisualMods(UIPainterContext painter)
        {
            if (_layoutMods != null)
            {
                foreach (IUILayoutModifier mod in _layoutMods)
                {
                    mod.ModifyVisual(painter);
                }
            }
        }
        #endregion
        #region Event invokers
        [StackTraceHidden] internal void Invoke_OnMouseMove(Vector2 position) => OnMouseMove?.Invoke(position);
        [StackTraceHidden] internal void Invoke_OnMouseDown(MouseButton button) => OnMouseDown?.Invoke(button);
        [StackTraceHidden] internal void Invoke_OnMouseUp(MouseButton button) => OnMouseUp?.Invoke(button);
        [StackTraceHidden] internal void Invoke_OnMouseActivate(MouseButton button) => OnMouseActivate?.Invoke(button);
        [StackTraceHidden] internal void Invoke_OnMouseFocusLost(MouseButton button) => OnMouseFocusLost?.Invoke(button);
        [StackTraceHidden] internal void Invoke_OnDragStart(OnDragStartContext context) => OnDragStart?.Invoke(context);
        #endregion

        public virtual IInteractionShape? Shape => null;

        protected override StyleUpdater? StyleUpdater => WindowOwner?.StyleUpdater;
        protected override StyleProvider? StyleProvider => WindowOwner?.StyleProvider;

        public IWindow? WindowOwner => _windowOwner;
        public IWindowHost? Host => _windowOwner?.ParentHost;

        public UIElement Parent { get => _parent!; set => SetParent(value); }
        public IReadOnlyList<UIElement> Children => _children;

        public IReadOnlyList<IUILayoutModifier>? LayoutModifiers => _layoutMods;

        public UIStateFlags StateFlags => _stateFlags;

        public Boundaries ElementTreeBounds { get => _elementTreeBounds; internal set => _elementTreeBounds = value; }
        public Vector2 ChildExtents { get => _childExtents; internal set => _childExtents = value; }

        public Vector2 RelativeOffset { get => _relativeOffset; set => _relativeOffset = value; }
        public Vector2 CurrentSize { get => _currentSize; set => _currentSize = value; }

        public Vector2 ViewOffset { get => _viewOffset; set => _viewOffset = value; }
        public Vector2 ViewSize { get => _viewSize; set => _viewSize = value; }

        public Boundaries PixelCoordinates { get => _pixelCoordinates; internal set => _pixelCoordinates = value; }
        public Boundaries ViewCoordinates { get => _viewCoordinates; internal set => _viewCoordinates = value; }

        #region Styleable
        [EditableProperty(nameof(_id))] public string? Id { get => _id; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_isActive))] public bool IsActive { get => _isActive; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_isEnabled), UIStateFlags.InvalidAll)] public bool IsEnabled { get => _isEnabled; set => SetEditableProperty(value); }

        [EditableProperty(nameof(_clipDescendents), UIStateFlags.InvalidVisual)] public bool ClipDescendents { get => _clipDescendents; set => SetEditableProperty(value); }

        [EditableProperty(nameof(_scrollPosition), UIStateFlags.InvalidVisual)] public Vector2 ScrollPosition { get => _scrollPosition; set => SetEditableProperty(value); }

        [StyleableProperty(nameof(_position), UIStateFlags.InvalidLayout)] public UIValue2 Position { get => _position; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_size), UIStateFlags.InvalidLayout)] public UIValue2 Size { get => _size; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_anchor), UIStateFlags.InvalidLayout)] public Vector2 Anchor { get => _anchor; set => SetStyleProperty(value); }
        #endregion
        #region Events
        public event Action? OnDestroy;

        public event Action<Vector2>? OnMouseMove;

        public event Action<MouseButton>? OnMouseDown;
        public event Action<MouseButton>? OnMouseUp;
        public event Action<MouseButton>? OnMouseActivate;
        public event Action<MouseButton>? OnMouseFocusLost;

        public event Action<OnDragStartContext>? OnDragStart;
        #endregion
    }
}
