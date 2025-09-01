using Editor.LegacyGui.Data;
using Editor.LegacyGui.Elements;
using Microsoft.Extensions.ObjectPool;
using Primary.Rendering;
using System.Numerics;

namespace Editor.LegacyGui.Managers
{
    internal sealed class GuiDockingManager
    {
        private DefaultObjectPool<DockSpace> _dockSpacePool;

        private HashSet<DockSpace> _activeDockSpaces;
        private Dictionary<UIWindow, DockSpace> _dockSpacesActive;

        private List<DockSpace> _focusBasedDockSpaces;

        private DockSpace? _dockingDockSpace;
        private DockSpace? _lastFoundDockSpace;
        private DockPosition _attemptedDockPos;

        internal GuiDockingManager()
        {
            _dockSpacePool = new DefaultObjectPool<DockSpace>(new DockSpacePolicy(), 12);

            _activeDockSpaces = new HashSet<DockSpace>();
            _dockSpacesActive = new Dictionary<UIWindow, DockSpace>();

            _focusBasedDockSpaces = new List<DockSpace>();

            _dockingDockSpace = null;
            _lastFoundDockSpace = null;
            _attemptedDockPos = DockPosition.None;
        }

        internal DockSpace DockWindowEmpty(UIWindow window)
        {
            if (_dockSpacesActive.TryGetValue(window, out DockSpace? dockSpace))
            {
                return dockSpace;
            }

            dockSpace = _dockSpacePool.Get();
            dockSpace.SetFloating(true);

            dockSpace.DockNewWindow(window);

            _activeDockSpaces.Add(dockSpace);
            _focusBasedDockSpaces.Add(dockSpace);

            _dockSpacesActive[window] = dockSpace;
            return dockSpace;
        }

        internal void DestroyEmptyDockSpace(DockSpace dockSpace)
        {
            dockSpace.Parent?.RemoveDockedDockSpace(dockSpace);

            _activeDockSpaces.Remove(dockSpace);
            _focusBasedDockSpaces.Remove(dockSpace);

            _dockSpacePool.Return(dockSpace);
        }

        internal DockSpace? SplitWindowIntoEmptyDock(DockSpace dockSpace, UIWindow window)
        {
            Span<DockSpace.WindowTabData> tabs = dockSpace.Tabs;
            if (tabs.Length < 2)
                return null; //nothing to split

            _dockSpacesActive.Remove(window);

            dockSpace.RemoveDockedWindow(window);
            return DockWindowEmpty(window);
        }

        internal void SetDockSpaceNewFocus(DockSpace dockSpace)
        {
            if (_focusBasedDockSpaces[0] == dockSpace)
                return;

            _focusBasedDockSpaces.Remove(dockSpace);
            _focusBasedDockSpaces.Insert(0, dockSpace);
        }

        internal void UpdateDockPosition(DockSpace? dockSpace, Vector2 position)
        {
            if (dockSpace != _dockingDockSpace)
            {
                _lastFoundDockSpace?.UpdateDockingData(null, DockPosition.Tabbed);
                _lastFoundDockSpace = null;
            }

            _dockingDockSpace = dockSpace;

            if (dockSpace != null)
            {
                DockSpace? newDockSpace = null;
                foreach (DockSpace dock in _focusBasedDockSpaces)
                {
                    if (dock.RootWindow == null || dock == dockSpace || dock.Window == null)
                        continue;

                    Vector2 minimum = dock.RootWindow!.Position;
                    Vector2 maximum = minimum + dock.Size;

                    if (position.X >= minimum.X && position.Y >= minimum.Y && position.X <= maximum.X && position.Y <= maximum.Y)
                    {
                        DockPosition dockPosition = CalculateDockPosition(dock, position);
                        if (dockPosition == DockPosition.Tabbed && dockSpace.Tabs.Length > 1)
                            continue;
                        
                        dock.UpdateDockingData(dockSpace, dockPosition);

                        newDockSpace = dock;
                        _attemptedDockPos = dockPosition;

                        break;
                    }
                }

                if (newDockSpace != _lastFoundDockSpace)
                {
                    _lastFoundDockSpace?.UpdateDockingData(null, DockPosition.None);
                    _lastFoundDockSpace = newDockSpace;
                }
            }
        }

        internal void CommitAttemptedDock(DockSpace dockSpace)
        {
            if (_dockingDockSpace != dockSpace)
                return;

            if (_lastFoundDockSpace != null && _attemptedDockPos != DockPosition.None)
            {
                if (_attemptedDockPos == DockPosition.Tabbed)
                {
                    UIWindow window = dockSpace.Tabs[0].Window!;

                    dockSpace.RemoveDockedWindow(window);
                    _lastFoundDockSpace.DockNewWindow(window);

                    _dockSpacesActive[window] = _lastFoundDockSpace;

                    DestroyEmptyDockSpace(dockSpace);
                }
                else
                {
                    dockSpace.SetFloating(false);
                    _lastFoundDockSpace.DockDockSpace(dockSpace, _attemptedDockPos);

                    //_activeDockSpaces.Remove(dockSpace);
                    _lastFoundDockSpace.InvalidateLayout();
                }
            }

            _lastFoundDockSpace?.UpdateDockingData(null, DockPosition.None);

            _dockingDockSpace = null;
            _lastFoundDockSpace = null;
            _attemptedDockPos = DockPosition.None;
        }

        private static DockPosition CalculateDockPosition(DockSpace dockSpace, Vector2 position)
        {
            Vector2 size = dockSpace.Size;
            Vector2 halfSize = size * 0.5f;

            position -= dockSpace.Window!.Position;

            Array.Fill(_dockSpaceDistances, float.PositiveInfinity);

            _dockSpaceDistances[(int)DockPosition.OuterTop - 1] = GetPointDistance(new Vector2(halfSize.X - 30.0f, 10.0f), new Vector2(halfSize.X + 30.0f, 50.0f), position);
            _dockSpaceDistances[(int)DockPosition.OuterBottom - 1] = GetPointDistance(new Vector2(halfSize.X - 30.0f, size.Y - 10.0f), new Vector2(halfSize.X + 30.0f, size.Y - 50.0f), position);
            _dockSpaceDistances[(int)DockPosition.OuterLeft - 1] = GetPointDistance(new Vector2(10.0f, halfSize.Y - 20.0f), new Vector2(70.0f, halfSize.Y + 20.0f), position);
            _dockSpaceDistances[(int)DockPosition.OuterRight - 1] = GetPointDistance(new Vector2(size.X - 10.0f, halfSize.Y - 20.0f), new Vector2(size.X - 70.0f, halfSize.Y + 20.0f), position);

            _dockSpaceDistances[(int)DockPosition.Top - 1] = GetPointDistance(halfSize + new Vector2(-30.0f, -30.0f), halfSize + new Vector2(30.0f, -70.0f), position);
            _dockSpaceDistances[(int)DockPosition.Bottom - 1] = GetPointDistance(halfSize + new Vector2(-30.0f, 30.0f), halfSize + new Vector2(30.0f, 70.0f), position);
            _dockSpaceDistances[(int)DockPosition.Left - 1] = GetPointDistance(halfSize + new Vector2(-40.0f, -20.0f), halfSize + new Vector2(-100.0f, 20.0f), position);
            _dockSpaceDistances[(int)DockPosition.Right - 1] = GetPointDistance(halfSize + new Vector2(40.0f, -20.0f), halfSize + new Vector2(100.0f, 20.0f), position);
            _dockSpaceDistances[(int)DockPosition.Tabbed - 1] = GetPointDistance(halfSize + new Vector2(-30.0f, -20.0f), halfSize + new Vector2(30.0f, 20.0f), position);

            float minimumValue = float.PositiveInfinity;
            DockPosition currentPosition = DockPosition.None;

            for (int i = 0; i < _dockSpaceDistances.Length; i++)
            {
                if (_dockSpaceDistances[i] < minimumValue)
                {
                    minimumValue = _dockSpaceDistances[i];
                    currentPosition = (DockPosition)i + 1;
                }
            }

            return minimumValue != float.PositiveInfinity ? currentPosition : DockPosition.None;

            static float GetPointDistance(Vector2 min, Vector2 max, Vector2 point)
            {
                Boundaries boundaries = new Boundaries(min, max);
                Vector2 minimum = boundaries.Minimum - new Vector2(50.0f);
                Vector2 maximum = boundaries.Maximum + new Vector2(50.0f);

                if (point.X >= minimum.X && point.Y >= minimum.Y && point.X <= maximum.X && point.Y <= maximum.Y)
                {
                    return Vector2.DistanceSquared(boundaries.Center, point);
                }

                return float.PositiveInfinity;
            }
        }

        private static float[] _dockSpaceDistances = new float[9];

        internal HashSet<DockSpace> ActiveDockSpaces => _activeDockSpaces;

        private record struct DockSpacePolicy : IPooledObjectPolicy<DockSpace>
        {
            public DockSpace Create()
            {
                return new DockSpace(false);
            }

            public bool Return(DockSpace obj)
            {
                obj.ClearRemainingDockData();
                return true;
            }
        }
    }
}
