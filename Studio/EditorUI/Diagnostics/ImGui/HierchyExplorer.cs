using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Dock;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.GUI.ImGui;

namespace EditorUI.Diagnostics.ImGui
{
    public sealed class HierchyExplorer : IImGuiDrawer
    {
        private WidgetWindow? _widgetWindow;
        private bool _selectWindow;

        public HierchyExplorer()
        {
            _widgetWindow = null;
            _selectWindow = false;
        }

        public void Draw()
        {
            return;
            if (IMGUI.BeginWindow("Hierchy explorer"))
            {
                if (IMGUI.BeginMenuBar())
                {
                    if (IMGUI.Button("Select window"))
                    {
                        _selectWindow = true;
                    }

                    IMGUI.EndMenuBar();
                }

                if (_widgetWindow != null)
                {
                    TreeRecursive(_widgetWindow.RootWidget);

                    static void TreeRecursive(Widget widget)
                    {
                        var nodeState = IMGUI.TreeNode(widget.Id ?? widget.GetType().Name);
                        if (nodeState.IsNodeOpen)
                        {
                            foreach (Widget childWidget in widget.Children)
                            {
                                TreeRecursive(childWidget);
                            }

                            IMGUI.TreePop();
                        }
                    }
                }

                IMGUI.EndWindow();
            }

            if (_selectWindow)
            {
                if (IMGUI.BeginWindow("Select window", ImGuiWindowFlags.AlwaysOnTop | ImGuiWindowFlags.AlwaysResize))
                {
                    if (IMGUI.Button("Close"))
                        _selectWindow = false;

                    foreach (DockHost host in UIManager.Instance.DockManager.DockHosts)
                    {
                        foreach (DockBase dock in host.Docked)
                        {
                            if (dock.CurrentWindow is WidgetWindow widgetWindow)
                            {
                                if (IMGUI.Button(widgetWindow.GetType().Name, _widgetWindow == widgetWindow))
                                {
                                    _widgetWindow = widgetWindow;
                                    _selectWindow = false;
                                }
                            }
                        }
                    }

                    IMGUI.EndWindow();
                }
            }
        }
    }
}
