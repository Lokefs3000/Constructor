using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Diagnostics;
using Primary.Collections.ReadOnly;
using Primary.Mathematics;

namespace EditorUI.Dock
{
    public sealed class DockManager : IDisposable
    {
        private readonly UIManager _manager;

        private List<DockHost> _activeDockHosts;
        // Kept for example changing window positions to not have to create new host data.
        private DockHost? _reserveDockHost;

        private bool _disposedValue;

        internal DockManager(UIManager manager)
        {
            _manager = manager;

            _activeDockHosts = new List<DockHost>();
            _reserveDockHost = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (DockHost host in _activeDockHosts)
                    {
                        host.Dispose();
                    }

                    _activeDockHosts.Clear();

                    _reserveDockHost?.Dispose();
                    _reserveDockHost = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public WindowDock CreateWindowDock(DockFlags flags)
        {
            WindowDock dock = new WindowDock(flags, this, _manager.WindowManager);
            return dock;
        }

        public DockHost CreateHostForDock(DockBase dock, bool isPerpetual, Rect targetRect)
        {
            Guard.IsNull(dock.Host, "Cannot create a host for a dock that already has a host!");

            DockHost dockHost;
            if (!isPerpetual && _reserveDockHost != null)
            {
                dockHost = _reserveDockHost;
                _reserveDockHost = null;
            }
            else
            {
                dockHost = new DockHost(this, isPerpetual);
            }

            _activeDockHosts.Add(dockHost);

            dockHost.SetupForNewDock(dock, targetRect);
            return dockHost;
        }

        internal void HandleEmptyHost(DockHost host)
        {
            if (_reserveDockHost == null)
            {
                host.ClearData();

                _reserveDockHost = host;
            }
            else
            {
                host.Dispose();
            }

            _activeDockHosts.Remove(host);
        }

        public ROList<DockHost> DockHosts => _activeDockHosts;
    }
}
