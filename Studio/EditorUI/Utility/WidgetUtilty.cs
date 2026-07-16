using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Dock;
using EditorUI.Widgets;
using Primary.Mathematics;

namespace EditorUI.Utility
{
    public static class WidgetUtilty
    {
        public static Vector2 FindGlobalPosition(Widget widget, Vector2 position)
        {
            Widget? parentWidget = widget;
            while ((parentWidget = parentWidget.Parent) != null)
            {
                if (parentWidget is WindowRoot windowRoot)
                {
                    if (windowRoot.OwningWindow != null && windowRoot.OwningWindow.Parent != null)
                    {
                        DockBase windowDockBase = windowRoot.OwningWindow.Parent;
                        if (windowDockBase.Host != null)
                        {
                            return position + (windowDockBase.WindowRect.Position + windowDockBase.Host.OwnedWindow.Position).AsVector2();
                        }
                    }

                    break;
                }
                else if (parentWidget is ScrollView scrollView)
                {
                    position -= scrollView.ScrollPosition;
                }
            }

            return position;
        }
    }
}
