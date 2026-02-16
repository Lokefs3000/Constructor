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
        private UITextShaper _textShaper;

        private ObjectPool<LayoutHandler> _handlers;

        internal UILayoutManager()
        {
            _textShaper = new UITextShaper();

            _handlers = new ObjectPool<LayoutHandler>(new LayoutHandler.Policy(this));
        }

        internal void RecalculateLayout(UIDockHost host)
        {
            host.RecalculateLayout();

            foreach (UIWindow window in host.TabbedWindows)
            {
                if (Flags.HasFlag(window.InvalidFlags, UIInvalidationFlags.Layout))
                {
                    window.RemoveInvalidFlags(UIInvalidationFlags.Layout);
                    RecalculateLayout(window);
                }
            }

            foreach (UIDockHost dockedHost in host.DockedHosts)
            {
                if (Flags.HasFlag(dockedHost.InvalidationFlags, UIInvalidationFlags.Layout))
                {
                    dockedHost.RemoveInvalidFlags(UIInvalidationFlags.Layout);
                    RecalculateLayout(dockedHost);
                }
            }
        }

        internal void RecalculateLayout(UIWindow window)
        {
            LayoutHandler handler = _handlers.Get();
            handler.Handle(window.RootElement);

            _handlers.Return(handler);
        }

        public UITextShaper TextShaper => _textShaper;

        public static Boundaries GetStrokeBoundaries(Boundaries boundaries, UIStrokePosition position, float weight)
        {
            switch (position)
            {
                case UIStrokePosition.Inside: return boundaries;
                case UIStrokePosition.Center:
                    {
                        float halfWeight = weight * 0.5f;
                        return new Boundaries(boundaries.Minimum - new Vector2(halfWeight), boundaries.Maximum + new Vector2(halfWeight));
                    }
                case UIStrokePosition.Outside: return new Boundaries(boundaries.Minimum - new Vector2(weight), boundaries.Maximum + new Vector2(weight));
            }

            return boundaries;
        }

        
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
