using CommunityToolkit.HighPerformance;
using Editor.Gui.Decorator;
using Editor.Gui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Gui.Elements
{
    public class Element
    {
        private Element? _parent;
        private List<Element> _children;

        private List<IDecorator>? _decorators;

        protected Vector2 _position;
        protected Vector2 _size;

        protected bool _layoutInvalidated;

        public Element()
        {
            _parent = null;
            _children = new List<Element>();

            _decorators = null;

            _position = Vector2.Zero;
            _size = Vector2.Zero;

            _layoutInvalidated = true;
        }

        public void InvalidateLayout()
        {
            _layoutInvalidated = true;
            if (!(_parent?._layoutInvalidated ?? true))
                _parent.InvalidateLayout();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool DrawVisual(GuiCommandBuffer commandBuffer)
        {
            DecorateVisual(commandBuffer);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool RecalculateLayout()
        {
            _position = _parent?._position ?? Vector2.Zero;
            _size = _parent?._size ?? Vector2.Zero;

            DecorateLayout();
            return true;
        }

        protected virtual void SetParent(Element newParent)
        {
            if (newParent == _parent)
                return;

            _parent?._children.Remove(this);
            newParent._children.Add(this);

            _parent = newParent;
        }

        protected void DecorateLayout()
        {
            if (_decorators != null)
            {
                for (int i = 0; i < _decorators.Count; i++)
                {
                    _decorators[i].ModifyLayout(this);
                }
            }
        }

        protected void DecorateVisual(GuiCommandBuffer commandBuffer)
        {
            if (_decorators != null)
            {
                for (int i = 0; i < _decorators.Count; i++)
                {
                    _decorators[i].DrawVisual(this, commandBuffer);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool RecalculateLayoutInternal() { bool r = RecalculateLayout(); _layoutInvalidated = false; return r; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool DrawVisualInternal(GuiCommandBuffer commandBuffer) => DrawVisual(commandBuffer);

        public static void Destroy(Element element)
        {
            
        }

        public Element? Parent { get => _parent; set => SetParent(value ?? throw new ArgumentException(nameof(value))); }
        public IReadOnlyList<Element> Children => _children;

        internal bool IsLayoutInvalid => _layoutInvalidated;
    }
}
