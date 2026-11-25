using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Dock;
using EditorUI.Visual;
using EditorUI.Visual.Built;
using EditorUI.Windowing;
using Primary.Common;
using Primary.GUI.ImGui;
using Primary.Input;
using Primary.Mathematics;
using Primary.Timing;
using Primary.Windowing;

namespace EditorUI.Diagnostics.ImGui
{
    public sealed class GuiStatistics : IImGuiDrawer
    {
        public void Draw()
        {
            IMGUI.Style.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4.0f));
            IMGUI.BeginWindow("GuiStats",
                ImGuiWindowFlags.NoTitlebar |
                ImGuiWindowFlags.NoBackground |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.AlwaysResize |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.DontBringToFrontOnFocus |
                ImGuiWindowFlags.NoInput);

            ImGuiWindowState windowState = IMGUI.CurrentWindow!;
            windowState.Position = new Vector2(10.0f);

            bool isMouseHoveringWindow = new Boundaries(windowState.Position, windowState.Position + windowState.Size).IsWithin(InputSystem.Pointer.MousePosition);
            uint textColor = isMouseHoveringWindow ? 0xffffff40 : 0xffffffff;

            windowState.DrawList.DrawFilledRect(windowState.Position, windowState.Position + windowState.Size, isMouseHoveringWindow ? 0x20000000 : 0x80000000);

            IMGUI.Style.PushColor(ImGuiColorIdx.Text, new Color32(textColor));

            UIManager manager = UIManager.Instance;

            IMGUI.Text($"Frametime: {Time.DeltaTimeDouble * 1000.0:f4}ms ({1.0 / Time.DeltaTimeDouble:f1} fps)");
            IMGUI.Text($"Text cache size: {manager.TextManager.TextCache.CurrentCacheSize}");
            IMGUI.Text($"Input focus: {manager.InputManager.HoveredInteractable?.GetType().Name ?? "null"}");
            windowState.CursorPos += new Vector2(0.0f, 11.0f);

            foreach (ActivePaintBuild paintBuild in manager.VisualManager.ActivePaintBuilds)
            {
                DockHost? dockHost = null;
                foreach (DockHost activeDockHost in manager.DockManager.DockHosts)
                {
                    if (activeDockHost.OwnedWindow == paintBuild.Window)
                    {
                        dockHost = activeDockHost;
                        break;
                    }
                }

                Window window = paintBuild.Window;
                Painter paintData = paintBuild.Painter;

                IMGUI.Text($"{window.WindowTitle} (0x{window.WindowId:x4})");
                IMGUI.Indent();
                IMGUI.Text($"Mesh: vtx:{paintData.MeshBuilder.Vertices.Length} idx:{paintData.MeshBuilder.Indices.Length} jobs:{paintData.MeshBuilder.StartedJobsCount}");
                IMGUI.Text($"Data: {paintData.MeshBuilder.DataBuffer.Length}b");
                IMGUI.Text($"Segments: {paintData.MeshBuilder.Segments.Count}");
                IMGUI.Text($"Paint: cmds:{paintData.CommandCount} objs:{paintData.ObjectList.Count} clips:{paintData.ScissorList.Count}");

                if (dockHost != null)
                {
                    IMGUI.Text("Performance:");
                    IMGUI.Indent();
                    IMGUI.Text($"Gather time: {dockHost.VisualStatistics.GatherCommands.TotalMilliseconds}ms");
                    IMGUI.Text($"Build time: {dockHost.VisualStatistics.PaintBuildTime.TotalMilliseconds}ms");
                    IMGUI.Unindent();
                }

                IMGUI.Unindent();
            }

            windowState.CursorPos += new Vector2(0.0f, 11.0f);

            foreach (DockHost host in manager.DockManager.DockHosts)
            {
                foreach (DockBase dock in host.Docked)
                {
                    if (dock.CurrentWindow is WidgetWindow widgetWindow)
                    {
                        IMGUI.Text(widgetWindow.GetType().Name);
                        IMGUI.Indent();

                        IMGUI.Text("Layout:");

                        IMGUI.Indent();

                        IMGUI.Text($"Measure time: {widgetWindow.LayoutStatistics.MeasurePassTime.TotalMilliseconds}ms");
                        IMGUI.Text($"Layout time: {widgetWindow.LayoutStatistics.LayoutPassTime.TotalMilliseconds}ms");
                        IMGUI.Text($"Finish time: {widgetWindow.LayoutStatistics.FinishPassTime.TotalMilliseconds}ms");
                        IMGUI.Text($"Updated: {widgetWindow.LayoutStatistics.UpdatedWidgets}");
                        IMGUI.Text($"Failed: {widgetWindow.LayoutStatistics.FailedWidgets}");

                        IMGUI.Unindent();

                        IMGUI.Unindent();
                    }
                }
            }

            IMGUI.Style.PopColor();

            IMGUI.EndWindow();
            IMGUI.Style.PopStyleVar();
        }
    }
}
