using CommunityToolkit.HighPerformance;
using Editor.LegacyGui.Decorators;
using Editor.LegacyGui.Managers;
using Editor.Rendering.Gui;
using System.Runtime.CompilerServices;

namespace Editor.LegacyGui.Elements
{
    public class Element
    {
        protected List<IDecorator>? _decorators = null;

        protected Element? _parent;
        protected List<Element> _children;

        protected bool _layoutInvalid = true;
        protected bool _visualInvalid = true;

        public Element()
        {
            _parent = null;
            _children = new List<Element>();
        }

        protected virtual void Destroyed()
        {

        }

        public virtual void InvalidateLayout()
        {
            _layoutInvalid = true;
            if (!(_parent?._layoutInvalid ?? true)) //dont propagate if parent is already invalid because it SHOULD already have propagated
                _parent!.RecalculateLayout();
        }

        public virtual void InvalidateVisual()
        {
            _visualInvalid = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool RecalculateLayout()
        {
            if (_decorators != null)
            {
                for (int i = 0; i < _decorators.Count; i++)
                {
                    _decorators[i].Decorate(this);
                }
            }

            _layoutInvalid = false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void DrawVisual(GuiCommandBuffer commandBuffer)
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool HandleEvent(ref readonly UIEvent @event)
        {
            return false;
        }

        public T AddDecorator<T>() where T : class, IDecorator
        {
            _decorators ??= new List<IDecorator>();
            if (!_decorators.Exists((x) => x is T))
                _decorators.Add(Activator.CreateInstance<T>());
            return (T)_decorators.Last();
        }

        public void RemoveDecorator<T>() where T : class, IDecorator
        {
            int idx = _decorators?.FindIndex((x) => x is T) ?? -1;
            if (idx >= 0)
                _decorators!.RemoveAt(idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetDecorator<T>() where T : class, IDecorator
        {
            return (T?)_decorators?.Find((x) => x is T);
        }

        protected virtual void SetParent(Element? newParent)
        {
            if (_parent == newParent)
                return;

            _parent?.RemoveChild(this);
            newParent?.AddChild(this);

            _parent = newParent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddChild(Element child) => _children.Add(child);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveChild(Element child) => _children.Remove(child);

        public Element? Parent { get => _parent; set => SetParent(value); }
        public ReadOnlySpan<Element> Children => _children.AsSpan();

        public bool LayoutInvalid => _layoutInvalid;
        public bool VisualInvalid => _visualInvalid;
    }
}
