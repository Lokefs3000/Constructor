using Editor.UI.Datatypes;
using Editor.UI.Layout;
using Editor.UI.Visual;
using Primary.Common;
using Primary.GUI.ImGui;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public T? RemoveLayoutModifier<T>() where T : class, IUILayoutModifier
        {
            _layoutMods.RemoveWhere((x) => x is T, out IUILayoutModifier? mod);
            return Unsafe.As<T>(mod);
        }

        public void InvalidateSelf(UIInvalidationFlags flags)
        {
            if (FlagUtility.HasFlag(flags, UIInvalidationFlags.Visual))
            {
                if ((_invalidFlags | UIInvalidationFlags.Visual) == _invalidFlags)
                    _invalidVisualRegion = _transform.RenderCoordinates;
                else
                    _invalidVisualRegion = Boundaries.Combine(_invalidVisualRegion, _transform.RenderCoordinates);
            }

            _invalidFlags |= flags;

            if (_parent != null)
            {
                UIElement? head = _parent;
                do
                {
                    if (FlagUtility.HasFlag(head._invalidFlags, flags))
                        break;

                    head.InvalidateSelf(flags);
                } while ((head = head.Parent) != null);
            }
        }

        public virtual UIRecalcLayoutStatus RecalculateLayout(UILayoutManager manager, UIRecalcType type)
        {
            Debug.Assert(_windowOwner != null);

            if (_transform.Recalculate(GetParentRenderBoundaries()))
            {
                InvalidateSelf(UIInvalidationFlags.Visual);
            }

            return UIRecalcLayoutStatus.Finished;
        }

        public virtual bool DrawVisual(UICommandBuffer commandBuffer)
        {
            return true;
        }

        public Boundaries GetParentRenderBoundaries()
        {
            if (_parent == null)
                return new Boundaries(Vector2.Zero, _windowOwner!.ClientSize);
            else
                return _parent._transform.RenderCoordinates;
        }

        internal void SetTreeBounds(Boundaries boundaries) => _elementTreeBounds = boundaries;

        public UIWindow? WindowOwner => _windowOwner;

        public int ZIndex => _zIndex;

        public UIElement? Parent { get => _parent; set => SetParent(value); }
        public IReadOnlyList<UIElement> Children => _children;

        public UIInvalidationFlags InvalidFlags => _invalidFlags;
        public Boundaries InvalidVisualRegion { get => _invalidVisualRegion; internal set => _invalidVisualRegion = value; }

        public UITransform Transform => _transform;
        public Boundaries ElementTreeBounds => _elementTreeBounds;

        public IReadOnlyList<IUILayoutModifier> LayoutModifiers => _layoutMods;
    }
}
