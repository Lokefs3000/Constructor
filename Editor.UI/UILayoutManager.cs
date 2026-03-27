using Editor.UI.Elements;
using Editor.UI.Layout;
using Editor.UI.Modifiers;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Pooling;
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
        private HashSet<IWindowHost> _invalidHosts;

        internal UILayoutManager()
        {
            _handlers = new ObjectPool<LayoutHandler>(new LayoutHandler.Policy(this));
            _invalidHosts = new HashSet<IWindowHost>();
        }

        public void AddInvalidLayout(IWindowHost host) => _invalidHosts.Add(host);
        public void RemoveInvalidHost(IWindowHost host) => _invalidHosts.Remove(host);

        internal void RecalculateAll()
        {
            if (_invalidHosts.Count > 0)
            {
                while (_invalidHosts.Count > 0)
                {
                    IWindowHost host = _invalidHosts.First();

                    if (host is UIDockHost dockHost)
                    {
                        dockHost.RecalculateLayout();

                        foreach (UIWindow window in dockHost.TabbedWindows)
                        {
                            if (Flags.HasFlag(window.RootElement.StateFlags, UIStateFlags.InvalidLayout))
                            {
                                window.RootElement.RemoveStateFlags(UIStateFlags.InvalidLayout);
                                RecalculateLayout(window);
                            }
                        }

                        foreach (UIDockHost dockedHost in dockHost.DockedHosts)
                        {
                            if (Flags.HasFlag(dockedHost.InvalidationFlags, UIStateFlags.InvalidLayout))
                            {
                                dockedHost.RemoveStateFlags(UIStateFlags.InvalidLayout);
                                _invalidHosts.Add(dockedHost);
                            }
                        }
                    }
                    else
                    {
                        host.RecalculateLayout();

                        foreach (UIWindow window in host.Windows)
                        {
                            if (Flags.HasFlag(window.RootElement.StateFlags, UIStateFlags.InvalidLayout))
                            {
                                window.RootElement.RemoveStateFlags(UIStateFlags.InvalidLayout);
                                RecalculateLayout(window);
                            }
                        }
                    }

                    _invalidHosts.Remove(host);
                }
            }
        }

        internal void RecalculateLayout(UIWindow window)
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
