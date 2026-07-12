using System.Numerics;
using CommunityToolkit.Diagnostics;
using EditorUI.Input;
using EditorUI.Statistics;
using EditorUI.Styling;
using EditorUI.Visual;
using EditorUI.Windowing;
using Primary.Collections.ReadOnly;
using Primary.Mathematics;
using Primary.Windowing;

using WindowManager = Primary.Windowing.WindowManager;

namespace EditorUI.Dock
{
    public sealed class DockHost : ISingleStyledObject, IDisposable, IInputDispatcher
    {
        private readonly DockManager _dockManager;
        private readonly StylesheetProvider _stylesheetProvider;

        private readonly Window _ownedWindow;
        private readonly bool _isPerpetual;

        private DockBase? _rootDock;
        private HashSet<DockBase> _docks;

        private VisualStatistics _visualStats;

        private bool _disposedValue;

        internal DockHost(DockManager dockManager, bool isPerpetual)
        {
            _dockManager = dockManager;
            _stylesheetProvider = new StylesheetProvider(UIManager.Instance.ValueSerializer);

            _ownedWindow = WindowManager.Instance.CreateWindow("DockHost", new Int2(640, 360), CreateWindowFlags.Resizable | CreateWindowFlags.Hidden);
            _isPerpetual = isPerpetual;

            _rootDock = null;
            _docks = new HashSet<DockBase>();

            _visualStats = new VisualStatistics();

            _ownedWindow.WindowResized += OnWindowResize;

            UIManager.Instance.InputManager.BindInputDispatcher(_ownedWindow, this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    UIManager.Instance.InputManager.UnbindInputDispatcher(_ownedWindow, this);
                    _ownedWindow.WindowResized -= OnWindowResize;
                    _ownedWindow.Dispose();
                    _stylesheetProvider.Dispose();
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
                rootDock.AddStateFlags(StateFlags.SelfInvalidLayout);
        }

        internal void ClearData()
        {
            _rootDock = null;
            _docks.Clear();
        }

        internal void UpdateData()
        {
            foreach (DockBase dock in _docks)
            {
                dock.UpdateData();
            }
        }

        internal void RecalculateLayout()
        {
            if (_rootDock != null)
            {
                if (_ownedWindow.ClientSize != _rootDock.DockRect.Size)
                    _rootDock.AddStateFlags(StateFlags.SelfInvalidLayout);

                if (_rootDock.StateFlags.HasFlag(StateFlags.InvalidLayout))
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
            if (forceChildLayout || stateFlags.HasFlag(StateFlags.ThisLayout))
            {
                Rect layoutRect;
                if (dock == _rootDock)
                    layoutRect = new Rect(_ownedWindow.ClientSize);
                else
                    layoutRect = dock.DockRect;

                foreach (DockBase childDock in docks)
                {
                    Rect dockRect = Rect.Zero;

                    int space = childDock.Space;
                    switch (childDock.Side)
                    {
                        case DockingSide.Left:
                            {
                                space = Math.Min(space, layoutRect.Width - WindowDock.TabHeight);

                                dockRect = new Rect(layoutRect.Position, new Int2(space, layoutRect.Height));
                                layoutRect.X += space;
                                layoutRect.Width -= space;
                                break;
                            }
                        case DockingSide.Right:
                            {
                                space = Math.Min(space, layoutRect.Width - WindowDock.TabHeight);

                                dockRect = new Rect(new Int2(layoutRect.X + layoutRect.Width - space, layoutRect.Y), new Int2(space, layoutRect.Height));
                                layoutRect.Width -= space;
                                break;
                            }
                        case DockingSide.Top:
                            {
                                space = Math.Min(space, layoutRect.Height - WindowDock.TabHeight);

                                dockRect = new Rect(layoutRect.Position, new Int2(layoutRect.Width, space));
                                layoutRect.Y += space;
                                layoutRect.Height -= space;
                                break;
                            }
                        case DockingSide.Bottom:
                            {
                                space = Math.Min(space, layoutRect.Height - WindowDock.TabHeight);

                                dockRect = new Rect(new Int2(layoutRect.X, layoutRect.Y + layoutRect.Height - space), new Int2(layoutRect.Width, space));
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

            forceChildLayout = stateFlags.HasFlag(StateFlags.ThisLayout);
            foreach (DockBase childDock in docks)
            {
                if (forceChildLayout || childDock.StateFlags.HasFlag(StateFlags.InvalidLayout))
                    RecursiveLayoutDocks(childDock, forceChildLayout);
            }

            dock.RemoveStateFlags(StateFlags.SelfInvalidLayout);

            foreach (WindowBase window in dock.Windows)
            {
                if (window is WidgetWindow widgetWindow)
                    widgetWindow.RootWidget.AddStateFlags(StateFlags.SelfInvalidLayout);
            }
        }

        private void RecursivePaintDocks(DockBase dock, ref readonly PainterContext painter)
        {
            foreach (DockBase childDock in dock.Docks)
            {
                RecursivePaintDocks(childDock, in painter);
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

        public InputDispatcherRoot GetInteractable(Vector2 point)
        {
            if (_rootDock == null)
                return InputDispatcherRoot.Null;

            return SearchDocks(_rootDock, point.AsInt2());

            static InputDispatcherRoot SearchDocks(DockBase dockBase, Int2 point)
            {
                if (point >= dockBase.DockRect.Position && point <= dockBase.DockRect.Maximum)
                {
                    if (dockBase.WindowRect.Position.Y > point.Y)
                        return new InputDispatcherRoot(dockBase as IInteractable, -dockBase.DockRect.Position.AsVector2());
                    return new InputDispatcherRoot(dockBase.CurrentWindow?.RootWidget, -dockBase.WindowRect.Position.AsVector2());
                }

                foreach (DockBase childDocks in dockBase.Docks)
                {
                    InputDispatcherRoot ret = SearchDocks(childDocks, point);
                    if (ret.Interactable != null)
                    {
                        return ret;
                    }
                }

                return InputDispatcherRoot.Null;
            }
        }

        private void OnWindowResize(Int2 newSize)
        {
            if (_rootDock != null && _rootDock.DockRect.Size != newSize)
            {
                _rootDock.AddStateFlags(StateFlags.SelfInvalidLayout);
            }
        }

        public Window OwnedWindow => _ownedWindow;
        Window IInputDispatcher.Window => _ownedWindow;

        public Rect HostRect => new Rect(Int2.Zero, _ownedWindow.ClientSize);

        public bool IsPerpetual => _isPerpetual;

        public ROHashSet<DockBase> Docked => _docks;
        public DockBase? RootDock => _rootDock;

        public StateFlags RootStateFlags => _rootDock?.StateFlags ?? StateFlags.None;

        public VisualStatistics VisualStatistics { get => _visualStats; internal set => _visualStats = value; }

        public StylesheetProvider StylesheetProvider => _stylesheetProvider;

        StyledObject? ISingleStyledObject.StyledObject => _rootDock;

        StylesheetProvider ISingleStyledObject.StylesheetProvider => _stylesheetProvider;
        bool ISingleStyledObject.GetAllProperties => false;
    }
}
