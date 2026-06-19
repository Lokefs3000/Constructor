using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Visual;
using EditorUI.Windowing;
using Primary.Collections.ReadOnly;
using Primary.Mathematics;

namespace EditorUI.Dock
{
    public abstract class DockBase
    {
        protected StateFlags _stateFlags;
        protected DockFlags _dockFlags;

        protected DockHost? _parentHost;

        public DockBase(DockFlags flags)
        {
            _stateFlags = StateFlags.None;
            _dockFlags = flags;

            _parentHost = null;
        }

        public abstract bool TryDockInto(DockBase newDock, DockingSide side);
        public abstract bool TryFloat(Int2 targetSize);

        public abstract bool TryAddWindow(WindowBase window);
        public abstract bool TryRemoveWindow(WindowBase window);

        public abstract bool TryFocusWindow(WindowBase window);

        protected internal abstract void UpdateData();
        protected internal abstract void RecalculateLayout(Rect dockRect);
        protected internal abstract void PaintVisual(ref readonly PainterContext painter);

        protected internal abstract bool TrySetDockHost(DockHost? newHost);

        protected internal abstract bool TryAddDockChild(DockBase child);
        protected internal abstract bool TryRemoveDockChild(DockBase child);

        protected internal abstract void TryUpdateWindowFocus(WindowBase window);

        protected internal virtual void TryAddStateFlags(StateFlags flags)
        {
            _stateFlags |= flags;
        }

        protected internal virtual void TryRemoveStateFlags(StateFlags flags)
        {
            _stateFlags &= ~flags;
        }

        public StateFlags StateFlags => _stateFlags;
        public DockFlags DockFlags => _dockFlags;

        public abstract Rect DockRect { get; }
        public abstract Rect WindowRect { get; }

        public virtual DockHost? Host => _parentHost;
        public abstract DockBase? Parent { get; }

        public abstract DockingSide Side { get; set; }
        public abstract int Space { get; set; }

        public abstract ROList<WindowBase> Windows { get; }
        public abstract int ActiveWindow { get; }

        public WindowBase? CurrentWindow => ActiveWindow != -1 ? Windows[ActiveWindow] : null;

        public abstract ROList<DockBase> Docks { get; }
    }

    public enum DockingSide : byte
    {
        Left = 0,
        Right,
        Top,
        Bottom
    }

    public enum DockFlags : byte
    {
        None = 0,

        NoDocking = 1 << 0,
        SingleWindow = 1 << 1
    }
}
