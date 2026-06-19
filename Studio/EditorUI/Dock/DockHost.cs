using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Diagnostics;
using EditorUI.Visual;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.Windowing;

namespace EditorUI.Dock
{
    public sealed class DockHost : IDisposable
    {
        private readonly DockManager _dockManager;

        private readonly Window _ownedWindow;
        private readonly bool _isPerpetual;

        private DockBase? _rootDock;
        private HashSet<DockBase> _docks;

        private bool _disposedValue;

        internal DockHost(DockManager dockManager, bool isPerpetual)
        {
            _dockManager = dockManager;

            _ownedWindow = WindowManager.Instance.CreateWindow("DockHost", new Int2(640, 360), CreateWindowFlags.Resizable | CreateWindowFlags.Hidden);
            _isPerpetual = isPerpetual;

            _rootDock = null;
            _docks = new HashSet<DockBase>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _ownedWindow.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void SetupForNewDock(DockBase rootDock, Rect targetRect)
        {
            _rootDock = rootDock;
            // _docks should is already empty

            RegisterNewDock(rootDock);

            Display display = WindowManager.Instance.GetNearestDisplayForPoint(targetRect)!;

            _ownedWindow.Position = Int2.Clamp(targetRect.Position, display.UsableBoundaries.Position, display.UsableBoundaries.Maximum - Int2.One);
            _ownedWindow.ClientSize = Int2.Clamp(targetRect.Size, new Int2(200), _ownedWindow.Display.UsableBoundaries.Size);
            _ownedWindow.Show();

            if (rootDock.DockRect.Size != _ownedWindow.ClientSize)
                rootDock.TryAddStateFlags(StateFlags.SelfInvalidLayout);
        }

        internal void ClearData()
        {
            _rootDock = null;
            _docks.Clear();
        }

        internal void RecalculateLayout()
        {
            if (_rootDock != null)
            {
                if (_ownedWindow.ClientSize != _rootDock.DockRect.Size)
                    _rootDock.TryAddStateFlags(StateFlags.SelfInvalidLayout);

                if (_rootDock.StateFlags.HasFlags(StateFlags.InvalidLayout))
                    RecursiveLayoutDocks(_rootDock, false);
            }
        }

        internal void PaintVisual(ref readonly PainterContext painter)
        {
            if (_rootDock != null)
            {
                RecursivePaintDocks(_rootDock, in painter);
            }
        }

        private void RecursiveLayoutDocks(DockBase dock, bool forceChildLayout)
        {
            StateFlags stateFlags = dock.StateFlags;

            ROList<DockBase> docks = dock.Docks;
            if (forceChildLayout || stateFlags.HasFlags(StateFlags.ThisLayout))
            {
                Rect layoutRect = dock.Parent?.DockRect ?? new Rect(_ownedWindow.ClientSize);

                foreach (DockBase childDock in docks)
                {
                    Rect dockRect = Rect.Zero;

                    int space = childDock.Space;
                    switch (childDock.Side)
                    {
                        case DockingSide.Left:
                            {
                                dockRect = new Rect(layoutRect.Position, new Int2(space, layoutRect.Height));
                                layoutRect.X += space;
                                break;
                            }
                        case DockingSide.Right:
                            {
                                dockRect = new Rect(new Int2(layoutRect.X - space, layoutRect.Y), layoutRect.Maximum);
                                layoutRect.Width -= space;
                                break;
                            }
                        case DockingSide.Top:
                            {
                                dockRect = new Rect(layoutRect.Position, new Int2(layoutRect.Width, space));
                                layoutRect.Y += space;
                                break;
                            }
                        case DockingSide.Bottom:
                            {
                                dockRect = new Rect(new Int2(layoutRect.X, layoutRect.Y - space), layoutRect.Maximum);
                                layoutRect.Height -= space;
                                break;
                            }
                    }

                    if (childDock.DockRect != dockRect)
                        childDock.RecalculateLayout(dockRect);
                }

                if (dock.DockRect != layoutRect)
                    dock.RecalculateLayout(layoutRect);
            }

            forceChildLayout = stateFlags.HasFlags(StateFlags.ThisLayout);
            foreach (DockBase childDock in docks)
            {
                if (forceChildLayout || childDock.StateFlags.HasFlags(StateFlags.InvalidLayout))
                    RecursiveLayoutDocks(childDock, forceChildLayout);
            }

            dock.TryRemoveStateFlags(StateFlags.SelfInvalidLayout);
        }

        private void RecursivePaintDocks(DockBase dock, ref readonly PainterContext painter)
        {
            foreach (DockBase childDock in dock.Docks)
            {
                RecursivePaintDocks(dock, in painter);
            }

            dock.PaintVisual(in painter);
        }

        public void RegisterNewDock(DockBase dock)
        {
            RecursiveRegister(dock);

            void RecursiveRegister(DockBase dock)
            {
                if (_docks.Add(dock))
                    dock.TrySetDockHost(this);

                foreach (DockBase childDock in dock.Docks)
                {
                    RecursiveRegister(childDock);
                }
            }
        }

        public void UnregisterDock(DockBase dock)
        {
            RecursiveUnregister(dock);

            // unregistering root dock means this host is no longer needed
            if (dock == _rootDock && !_isPerpetual)
            {
                Guard.IsEqualTo(_docks.Count, 0, "Expected all child docks to be unregistered when unregistering root dock!");
                _dockManager.HandleEmptyHost(this);
            }

            void RecursiveUnregister(DockBase dock)
            {
                if (_docks.Remove(dock))
                {
                    Guard.IsTrue(dock.Host == this, "The dock must unregister itself with it's host before it registers in a new one!");
                    dock.TrySetDockHost(null);
                }

                foreach (DockBase childDock in dock.Docks)
                {
                    RecursiveUnregister(childDock);
                }
            }
        }

        public void FocusDockHost()
        {
            _ownedWindow.TakeFocus();
        }

        public Window OwnedWindow => _ownedWindow;

        public Rect HostRect => new Rect(Int2.Zero, _ownedWindow.ClientSize);

        public bool IsPerpetual => _isPerpetual;

        public ROHashSet<DockBase> Docked => _docks;
        public DockBase? RootDock => _rootDock;

        public StateFlags RootStateFlags => _rootDock?.StateFlags ?? StateFlags.None;
    }
}
