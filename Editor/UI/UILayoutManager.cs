using Editor.UI.Elements;
using Editor.UI.Layout;
using Editor.UI.Text;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;
using TerraFX.Interop.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Editor.UI
{
    public sealed class UILayoutManager
    {
        private UITextShaper _textShaper;

        internal UILayoutManager()
        {
            _textShaper = new UITextShaper();
        }

        internal void RecalculateLayout(UIDockHost host)
        {
            host.RecalculateLayout();

            foreach (UIWindow window in host.TabbedWindows)
            {
                if (FlagUtility.HasFlag(window.InvalidFlags, UIInvalidationFlags.Layout))
                {
                    window.RemoveInvalidFlags(UIInvalidationFlags.Layout);
                    RecalculateLayout(window);
                }
            }

            foreach (UIDockHost dockedHost in host.DockedHosts)
            {
                if (FlagUtility.HasFlag(dockedHost.InvalidationFlags, UIInvalidationFlags.Layout))
                {
                    dockedHost.RemoveInvalidFlags(UIInvalidationFlags.Layout);
                    RecalculateLayout(dockedHost);
                }
            }
        }

        internal void RecalculateLayout(UIWindow window)
        {
            Queue<UIElement> fwdElements = new Queue<UIElement>();
            Stack<ReverseData> revElements = new Stack<ReverseData>();

            fwdElements.Enqueue(window.RootElement);

            while (fwdElements.TryDequeue(out UIElement? element))
            {
                UIRecalcLayoutStatus status = element.RecalculateLayout(this, UIRecalcType.Descending);
                if (status == UIRecalcLayoutStatus.Finished || status == UIRecalcLayoutStatus.PartiallyFinished)
                {
                    if (status == UIRecalcLayoutStatus.Finished)
                        element.RemoveInvalidFlag(UIInvalidationFlags.Layout);

                    bool needsRevTiming = false;
                    foreach (IUILayoutModifier modifiers in element.LayoutModifiers)
                    {
                        if (FlagUtility.HasFlag(modifiers.Timing, IUILayoutModiferTime.Acending))
                            needsRevTiming = true;
                        if (FlagUtility.HasFlag(modifiers.Timing, IUILayoutModiferTime.Descending))
                            modifiers.ModifyElement(IUILayoutModiferTime.Descending);
                    }

                    revElements.Push(new ReverseData(element, status == UIRecalcLayoutStatus.PartiallyFinished ? UIReverseHandling.Full : (needsRevTiming ? UIReverseHandling.OnlyMods : UIReverseHandling.None)));

                    foreach (UIElement child in element.Children)
                    {
                        if (FlagUtility.HasFlag(child.InvalidFlags, UIInvalidationFlags.Layout))
                            fwdElements.Enqueue(child);
                    }
                }
            }

            while (revElements.TryPop(out ReverseData data))
            {
                if (data.Handling >= UIReverseHandling.OnlyMods)
                {
                    foreach (IUILayoutModifier modifiers in data.Element.LayoutModifiers)
                    {
                        if (FlagUtility.HasFlag(modifiers.Timing, IUILayoutModiferTime.Acending))
                            modifiers.ModifyElement(IUILayoutModiferTime.Acending);
                    }
                }

                if (data.Handling >= UIReverseHandling.Full || FlagUtility.HasFlag(data.Element.InvalidFlags, UIInvalidationFlags.Layout))
                {
                    data.Element.RecalculateLayout(this, UIRecalcType.Ascending);
                    data.Element.RemoveInvalidFlag(UIInvalidationFlags.Layout);
                }

                {
                    Boundaries bounds = data.Element.Transform.RenderCoordinates;
                    if (data.Element.Children.Count > 0)
                    {
                        foreach (UIElement child in data.Element.Children)
                        {
                            bounds = Boundaries.Combine(bounds, child.Transform.RenderCoordinates);
                        }
                    }

                    data.Element.SetTreeBounds(bounds);
                }
            }
        }

        public UITextShaper TextShaper => _textShaper;

        private readonly record struct ReverseData(UIElement Element, UIReverseHandling Handling);
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
