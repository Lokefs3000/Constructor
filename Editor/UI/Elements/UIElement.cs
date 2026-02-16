using Editor.UI.Datatypes;
using Editor.UI.Debugging;
using Editor.UI.Interaction;
using Editor.UI.Layout;
using Editor.UI.Modifiers;
using Editor.UI.Visual;
using Primary.Common;
using Primary.GUI.ImGui;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace Editor.UI.Elements
{
    public class UIElement
    {
        private UIWindow? _windowOwner;

        private int _zIndex;

        private UIElement? _parent;
        private List<UIElement> _children;

        private UIInvalidationFlags _invalidFlags;
        private Boundaries _invalidVisualRegion;

        private UITransform _transform;
        private Boundaries _elementTreeBounds;

        private List<IUILayoutModifier> _layoutMods;

        private string? _id;

        public UIElement(UIElement? parent = null)
        {
            _windowOwner = null;

            _zIndex = 0;

            _parent = null;
            _children = new List<UIElement>();

            _invalidFlags = UIInvalidationFlags.None;
            _invalidVisualRegion = Boundaries.Zero;

            _transform = new UITransform(this);
            _elementTreeBounds = Boundaries.Zero;

            _layoutMods = new List<IUILayoutModifier>();

            _id = null;

            if (parent != null)
                parent.AddChild(this);
        }

        internal void AddChild(UIElement child)
        {
            if (child._parent == this)
                return;
            Debug.Assert(!_children.Contains(child));

            _children.Add(child);
        }

        internal void RemoveChild(UIElement child)
        {
            if (child._parent != this)
                return;
            Debug.Assert(_children.Contains(child));

            _children.Remove(child);
        }

        public void MoveChild(UIElement child, int newIndex)
        {
            if (_children.IndexOf(child) != newIndex && _children.Remove(child))
            {
                _children.Insert(newIndex, child);
                InvalidateSelf(UIInvalidationFlags.Layout);
            }
        }

        internal void RemoveInvalidFlag(UIInvalidationFlags flags)
        {
            _invalidFlags &= ~flags;
        }

        internal void SetNewAndUpdateChildren(UIWindow? newOwner, int zIndex)
        {
            if (_windowOwner != newOwner)
            {
                _windowOwner = newOwner;
                _zIndex = zIndex;

                if (_children.Count > 0)
                {
                    ++zIndex;
                    foreach (UIElement child in _children)
                    {
                        if (child._windowOwner != newOwner)
                        {
                            child.SetNewAndUpdateChildren(newOwner, zIndex);
                        }
                    }
                }
            }
        }

        public void SetParent(UIElement? newParent)
        {
            if (_parent == newParent)
                return;

            _parent?.RemoveChild(this);
            newParent?.AddChild(this);

            _parent = newParent;

            SetNewAndUpdateChildren(newParent?._windowOwner, newParent == null ? 0 : newParent._zIndex + 1);
            InvalidateSelf(UIInvalidationFlags.Layout);
        }

        public T AddLayoutModifier<T>() where T : class, IUILayoutModifier
        {
            T mod = (T)Activator.CreateInstance(typeof(T), [this])!;
            _layoutMods.Add(mod);

            return mod;
        }

        public IUILayoutModifier AddLayoutModifier(Type type)
        {
            IUILayoutModifier mod = (IUILayoutModifier)Activator.CreateInstance(type, [this])!;
            _layoutMods.Add(mod);

            return mod;
        }

        public T? RemoveLayoutModifier<T>() where T : class, IUILayoutModifier
        {
            _layoutMods.RemoveWhere((x) => x is T, out IUILayoutModifier? mod);
            return Unsafe.As<T>(mod);
        }

        public IUILayoutModifier? RemoveLayoutModifier(Type type)
        {
            _layoutMods.RemoveWhere((x) => x.GetType() == type, out IUILayoutModifier? mod);
            return mod;
        }

        public void InvalidateSelf(UIInvalidationFlags flags)
        {
            if (Flags.HasFlag(flags, UIInvalidationFlags.Visual))
            {
                if ((_invalidFlags | UIInvalidationFlags.Visual) == _invalidFlags)
                    _invalidVisualRegion = _transform.RenderCoordinates;
                else
                    _invalidVisualRegion = Boundaries.Combine(_invalidVisualRegion, _transform.RenderCoordinates);
            }

            _invalidFlags |= flags;

            if (_parent != null)
            {
                flags &= UIInvalidationFlags.All;

                UIElement? head = _parent;
                do
                {
                    head.InvalidateSelf(flags);
                } while ((head = head.Parent) != null);
            }
            else if (_windowOwner != null)
            {
                _windowOwner.ParentHost?.InvalidateSelf(_invalidFlags);
            }
        }

        public virtual void MeasureSize(UILayoutManager manager)
        {
            Debug.Assert(_windowOwner != null);

            if (_transform.Recalculate(GetParentSize()))
            {
                InvalidateSelf(UIInvalidationFlags.Visual);
            }
        }

        public virtual void RecalculateLayout(UILayoutManager manager, ref UIMeasurements measurements)
        {
            ExecuteLayoutModifiers(ref measurements);
        }

        public virtual bool DrawVisual(UICommandBuffer commandBuffer)
        {
            ExecuteDrawModifiers(commandBuffer);
            return true;
        }

        public virtual void HandleEvent(HostInteractionManager interaction, ref readonly UIEvent @event)
        {

        }

        public virtual void AddStateToRecorder(LayoutRecorder recorder)
        {
            recorder.AddValue(this, "RelativePosition", _transform.RelativePosition);
        }

        internal void ExecuteMeasureModifiers(Vector2 treeSize)
        {
            foreach (IUILayoutModifier modifier in _layoutMods)
            {
                modifier.MeasureSize(treeSize);
            }
        }

        protected void ExecuteLayoutModifiers(ref UIMeasurements measurements)
        {
            foreach (IUILayoutModifier modifier in _layoutMods)
            {
                measurements.CheckIfTreeIsValid();
                modifier.ModifyElement(ref measurements);
            }
        }

        internal void ExecuteDrawModifiers(UICommandBuffer commandBuffer)
        {
            foreach (IUILayoutModifier modifier in _layoutMods)
            {
                modifier.DrawVisual(commandBuffer);
            }
        }

        public T? FindElementWithId<T>(string id) where T : UIElement
        {
            foreach (UIElement child in _children)
            {
                if (child is T && child.Id == id)
                    return Unsafe.As<T>(child);
                else
                {
                    T? ret = child.FindElementWithId<T>(id);
                    if (ret != null)
                        return ret;
                }
            }

            return null;
        }

        public Vector2 GetParentSize() => _parent?.GetSizeAsParent() ?? _windowOwner!.ClientSize;

        protected virtual Vector2 GetSizeAsParent() => _transform.RealSize;

        protected void SetRealSize(Vector2 realSize) => _transform.RealSize = realSize;

        internal void SetTreeBounds(Boundaries boundaries) => _elementTreeBounds = boundaries;
        internal void UpdateTransform() => _transform.Recalculate(_parent?.Transform?.RealSize ?? _windowOwner!.ClientSize);
        internal void ComputeBoundaries() => _transform.ComputeBoundaries(_parent?._transform?.RenderCoordinates.Minimum ?? Vector2.Zero);

        public UIWindow? WindowOwner => _windowOwner;

        public int ZIndex => _zIndex;

        public UIElement? Parent { get => _parent; set => SetParent(value); }
        public IReadOnlyList<UIElement> Children => _children;

        public UIInvalidationFlags InvalidFlags => _invalidFlags;
        public Boundaries InvalidVisualRegion { get => _invalidVisualRegion; internal set => _invalidVisualRegion = value; }

        public UITransform Transform => _transform;
        public Boundaries ElementTreeBounds => _elementTreeBounds;

        public IReadOnlyList<IUILayoutModifier> LayoutModifiers => _layoutMods;

        public string? Id { get => _id; set => _id = value; }
    }
}
