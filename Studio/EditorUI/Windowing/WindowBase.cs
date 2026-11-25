using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using EditorUI.Dock;
using EditorUI.Input;
using EditorUI.Styling;
using EditorUI.Visual;
using EditorUI.Widgets;
using Primary.Mathematics;

namespace EditorUI.Windowing
{
    public abstract class WindowBase
    {
        public WindowBase()
        {
        }

        public abstract bool TryDockInto(DockBase dock);
        public abstract bool TryFloat(Int2 targetSize);

        public abstract void UpdateData();

        public abstract void TakeFocus();
        public abstract void Close();

        internal protected abstract void OnFocusGained();
        internal protected abstract void OnFocusLost();

        internal protected abstract void RecalculateLayout(Rect windowRect);
        internal protected abstract void PaintOverlay(ref readonly PainterContext painter);
        internal protected abstract void HandleEvent(IInteractable interactable, ref readonly UIInputEvent inputEvent);

        internal protected abstract void DestroySelf();

        protected internal abstract void TryAddStateFlags(StateFlags flags);
        protected internal abstract void TryRemoveStateFlags(StateFlags flags);

        public abstract StateFlags StateFlags { get; }

        public abstract Rect WindowRect { get; }

        public abstract DockBase? Parent { get; }

        public abstract StylesheetProvider StylesheetProvider { get; }
        public abstract Widget RootWidget { get; }

        public abstract event Action<IInteractable, ReadOnlyRef<UIInputEvent>>? OnEventDispatched;
    }
}
