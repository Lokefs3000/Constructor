using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using CommunityToolkit.Diagnostics;
using EditorUI.Visual;
using EditorUI.Windowing;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.Utility;

namespace EditorUI.Dock
{
    public sealed class WindowDock : DockBase
    {
        private readonly DockManager _dockManager;
        private readonly WindowManager _windowManager;

        private DockBase? _parentDock;

        private DockingSide _dockingSide;
        private int _dockingSpace;

        private Rect _dockRect;
        private Rect _windowRect;

        private List<WindowBase> _windows;
        private List<DockBase> _docks;

        private int _activeWindow;

        internal WindowDock(DockFlags flags, DockManager dockManager, WindowManager windowManager) : base(flags)
        {
            _dockManager = dockManager;
            _windowManager = windowManager;

            _parentDock = null;

            _dockingSide = DockingSide.Left;
            _dockingSpace = 0;

            _dockRect = Rect.Zero;
            _windowRect = Rect.Zero;

            _windows = new List<WindowBase>();
            _docks = new List<DockBase>();

            _activeWindow = -1;
        }

        private void DestroySelf()
        {
            Guard.IsEmpty(_windows);

            if (_parentDock != null)
            {
                int i = 0;
                while (_docks.Count > i)
                {
                    DockBase dock = _docks[i];
                    if (!dock.TryDockInto(_parentDock, dock.Side))
                        ++i;
                }

                _docks.Clear();

                _parentDock = null;
            }

            _parentHost?.UnregisterDock(this);
            _parentHost = null;
        }

        public override bool TryDockInto(DockBase newDock, DockingSide side)
        {
            // fail because we can't be parented to ourselves
            if (newDock == this)
                return false;

            // succeed because this is already valid
            if (newDock == _parentDock)
                return true;

            _parentDock?.TryRemoveDockChild(this);

            if (newDock.TryAddDockChild(this))
            {
                _parentDock = newDock;

                if (newDock.Host != _parentHost)
                {
                    _parentHost?.UnregisterDock(this);
                    newDock.Host?.RegisterNewDock(this);
                }

                TryAddStateFlags(StateFlags.SelfInvalidLayout);
                return true;
            }
            else
            {
                _parentHost?.UnregisterDock(this);

                _parentDock = null;
                _parentHost = null;
            }

            return false;
        }

        public override bool TryFloat(Int2 targetSize)
        {
            throw new NotImplementedException();
        }

        public override bool TryAddWindow(WindowBase window)
        {
            if (_windows.Count > 0 && _dockFlags.HasFlags(DockFlags.SingleWindow))
                return false;

            if (_windows.AddUnique(window))
            {
                if (_activeWindow == -1)
                    TryFocusWindow(window);

                TryAddStateFlags(StateFlags.SelfInvalidLayout);
            }

            return true;
        }

        public override bool TryRemoveWindow(WindowBase window)
        {
            int windowIndex = _windows.IndexOf(window);
            if (windowIndex != -1)
            {
                if (_activeWindow == windowIndex)
                {
                    if (_windows.Count == 1)
                        TryFocusWindow(null);
                    else
                        TryFocusWindow(_windows[_activeWindow]);
                }

                _windows.RemoveAt(windowIndex);
                
                // if (_windows.Count == 0)
                // {
                //     DestroySelf();
                // }

                TryAddStateFlags(StateFlags.SelfInvalidLayout);
                return true;
            }

            return false;
        }

        public override bool TryFocusWindow(WindowBase? window)
        {
            if (window == null)
            {
                if (_activeWindow != -1)
                {
                    _windowManager.TrySetWindowFocus(null);
                    return true;
                }

                return false;
            }

            int windowIndex = _windows.IndexOf(window);
            if (windowIndex != -1)
            {
                if (_activeWindow != windowIndex)
                {
                    _windowManager.TrySetWindowFocus(_windows[windowIndex]);
                }

                _activeWindow = windowIndex;
                return true;
            }

            return false;
        }

        protected internal override void UpdateData()
        {
            if (_activeWindow != -1 && _windows.Count > 0)
            {
                WindowBase window = _windows[_activeWindow];
                window.UpdateData();

                StateFlags windowStateFlags = window.StateFlags;
                if (windowStateFlags != StateFlags.None)
                {
                    TryAddStateFlags(windowStateFlags.RemoveFlags(StateFlags.This));
                }
            }
        }

        protected internal override void RecalculateLayout(Rect dockRect)
        {
            _dockRect = dockRect;
            _windowRect = _dockFlags.HasFlags(DockFlags.SingleWindow) ? dockRect : Rect.OffsetMin(dockRect, 0, TabHeight);
        }

        protected internal override void PaintVisual(PainterContext painter)
        {
            if (_dockFlags.HasFlags(DockFlags.SingleWindow))
                return;

            painter.AddRectangle(new Boundaries(Vector2.Zero, new Vector2(_dockRect.Width, TabHeight)), new Paint(Color.Yellow));

            painter.AddCircle(new Vector2(_dockRect.Width, TabHeight) * 0.5f, TabHeight * 0.5f, new Paint(Color.Blue));
            painter.AddCircle(new Vector2(_dockRect.Width, TabHeight) * 0.5f - new Vector2(TabHeight * 1.25f, 0.0f), TabHeight * 0.25f, new Paint(Color.Blue));
            painter.AddCircle(new Vector2(_dockRect.Width, TabHeight) * 0.5f + new Vector2(TabHeight * 1.25f, 0.0f), TabHeight * 0.25f, new Paint(Color.Blue));
        }

        protected internal override bool TrySetDockHost(DockHost? newHost)
        {
            if (_parentHost == newHost)
                return true;

            _parentHost?.UnregisterDock(this);
            _parentHost = newHost;

            return true;
        }

        protected internal override bool TryAddDockChild(DockBase child)
        {
            // this dock cannot be docked into
            if (_dockFlags.HasFlags(DockFlags.NoDocking))
                return false;

            if (child == this)
                return false;
            
            if (_docks.AddUnique(child))
                TryAddStateFlags(StateFlags.SelfInvalidLayout);

            return true;
        }

        protected internal override bool TryRemoveDockChild(DockBase child)
        {
            if (child == this)
                return false;

            if (_docks.Remove(child))
            {
                TryAddStateFlags(StateFlags.SelfInvalidLayout);
                return true;
            }

            return false;
        }

        protected internal override void TryUpdateWindowFocus(WindowBase window)
        {
            if (_windows.Contains(window))
            {
                _parentHost?.FocusDockHost();
            }
        }

        protected internal override void TryAddStateFlags(StateFlags flags)
        {
            base.TryAddStateFlags(flags);

            if (_parentDock != null)
                _parentDock.TryAddStateFlags(flags);
        }

        public override Rect DockRect => _dockRect;
        public override Rect WindowRect => _windowRect;

        public override DockBase? Parent => _parentDock;

        public override DockingSide Side
        {
            get => _dockingSide;
            set
            {
                if (_dockingSide != value)
                {
                    _dockingSide = value;
                    TryAddStateFlags(StateFlags.SelfInvalidLayout);
                }
            }
        }
        public override int Space
        {
            get => _dockingSpace;
            set
            {
                int clampedSpace = Math.Max(value, WindowDock.TabHeight);
                if (_dockingSpace != clampedSpace)
                {
                    _dockingSpace = clampedSpace;
                    TryAddStateFlags(StateFlags.SelfInvalidLayout);
                }
            }
        }

        public override ROList<WindowBase> Windows => _windows;
        public override int ActiveWindow => _activeWindow;

        public override ROList<DockBase> Docks => _docks;

        public const int TabHeight = 24;
    }
}
