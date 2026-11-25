using Editor.UI.Elements;
using Editor.UI.Layout;
using Editor.UI.Modifiers;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Pooling;
using Primary.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using TerraFX.Interop.Windows;

namespace Editor.UI
{
    public sealed class UILayoutManager
    {
        private ObjectPool<LayoutHandler> _handlers;
        private HashSet<ILayoutHost> _invalidHosts;

        internal UILayoutManager()
        {
            _handlers = new ObjectPool<LayoutHandler>(new LayoutHandler.Policy(this));
            _invalidHosts = new HashSet<ILayoutHost>();
        }

        public void AddInvalidLayout(ILayoutHost host) => _invalidHosts.Add(host);
        public void RemoveInvalidHost(ILayoutHost host) => _invalidHosts.Remove(host);

        internal void RecalculateAll()
        {
            if (_invalidHosts.Count > 0)
            {
                while (_invalidHosts.Count > 0)
                {
                    ILayoutHost host = _invalidHosts.First();

                    host.RecalculateLayout();
                    if (host is IWindowHost windowHost)
                    {
                        IWindow? currentWindow = windowHost.ActiveWindow;
                        if (currentWindow != null && Flags.HasFlag(currentWindow.RootElement.StateFlags, UIStateFlags.InvalidLayout))
                        {
                            currentWindow.RootElement.RemoveStateFlags(UIStateFlags.InvalidLayout);
                            RecalculateLayout(currentWindow);
                        }

                        if (windowHost is IWindowDockHost windowDockHost)
                        {
                            foreach (IWindowHost childHost in windowDockHost.Hosts)
                            {
                                if (Flags.HasFlag(childHost.InvalidationFlags, UIStateFlags.InvalidLayout))
                                {
                                    childHost.RemoveStateFlags(UIStateFlags.InvalidLayout);
                                    _invalidHosts.Add(childHost);
                                }
                            }
                        }
                    }

                    _invalidHosts.Remove(host);
                }
            }
        }

        internal void RecalculateLayout(IWindow window)
        {
            LayoutHandler handler = _handlers.Get();
            handler.Handle(window.RootElement);

            _handlers.Return(handler);
        }

        public bool HasInvalidWindowHosts => _invalidHosts.Count > 0;
    }

    public enum UIRecalcLayoutStatus : byte
    {
        /// <summary>Finished</summary>
        Finished = 0,

        /// <summary>Stop traversing this branch of the tree</summary>
        EndBranchTraversal,

        /// <summary>Return later when going up again and try again</summary>
        PartiallyFinished
    }

    public enum UIRecalcType : byte
    {
        Descending = 0,
        Ascending
    }

    public enum UIReverseHandling : byte
    {
        None = 0,
        OnlyMods,
        Full
    }
}
