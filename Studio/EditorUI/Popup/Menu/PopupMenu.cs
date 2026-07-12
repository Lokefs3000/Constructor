using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using EditorUI.Assets;
using EditorUI.Input;
using EditorUI.Styling;
using EditorUI.Text;
using EditorUI.Visual;
using EditorUI.Widgets;
using Primary.Collections.ReadOnly;
using Primary.Windowing;

namespace EditorUI.Popup.Menu
{
    public abstract class PopupMenu : StyledObject, IInteractable
    {
        public abstract void Destroy();

        protected internal abstract void UpdateSelf(Window window);
        protected internal abstract void PaintSelf(ref readonly PainterContext painter, Vector2 originPosition, Window window);
        public abstract void HandleEventSelf(ref readonly UIInputEvent inputEvent);

        internal abstract void BeginItemHover(PopupMenuItem item);
        internal abstract void EndItemHover(PopupMenuItem item);
        internal abstract void HandleItemPress(PopupMenuItem item);

        internal abstract void Lock();
        internal abstract void Unlock();

        protected internal void ThrowIfLocked()
        {
            if (IsMenuLocked)
                throw new InvalidOperationException("No operations can be performed on this menu because it is currently locked");
        }

        public virtual IInteractable GetInteractable(Vector2 point) => this;

        public abstract StylesheetProvider StylesheetProvider { get; }

        public virtual IInteractionShape? Shape => null;
        public virtual WidgetInputState InputState => WidgetInputState.Sink;

        public abstract bool IsMenuLocked { get; }

        #region Serializable
        public abstract IAssetProvider<FontFamily>? FontFamily { get; set; }
        public abstract FontStyle FontStyle { get; set; }
        public abstract FontWeight FontWeight { get; set; }

        public abstract float FontSize { get; set; }

        public abstract Vector4 ItemPadding { get; set; }
        public abstract float MaxWidth { get; set; }
        #endregion
    }

    internal record struct PopupMenuStyledEnumerable(ROList<PopupMenuItem> Items) : IEnumerable<StyledObject>
    {
        public IEnumerator<StyledObject> GetEnumerator() => new WidgetChildEnumerator(Items);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private record struct WidgetChildEnumerator : IEnumerator<StyledObject>
        {
            private readonly ROList<PopupMenuItem> _items;
            private int _childIndex;

            private StyledObject? _value;

            public WidgetChildEnumerator(ROList<PopupMenuItem> items)
            {
                _items = items;
                _childIndex = 0;

                _value = null;
            }

            public void Dispose()
            {
                _childIndex = -1;
                _value = null;
            }

            public void Reset()
            {
                _childIndex = 0;
                _value = null;
            }

            public bool MoveNext()
            {
                if (_childIndex == -1)
                    return false;

                _value = _items[_childIndex++];

                if (_childIndex >= _items.Count)
                    _childIndex = -1;

                return true;
            }

            public readonly StyledObject Current => _value!;
            readonly object IEnumerator.Current => Current;
        }
    }
}
