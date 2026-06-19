using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Visual.Built;
using Primary.GUI.ImGui;

namespace EditorUI.Diagnostics.ImGui
{
    public sealed class GuiStatistics : IImGuiDrawer
    {
        public void Draw()
        {
            IMGUI.BeginWindow("GuiStats",
                ImGuiWindowFlags.NoTitlebar |
                ImGuiWindowFlags.NoBackground |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.AlwaysResize |
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.DontBringToFrontOnFocus);

            ImGuiWindowState windowState = IMGUI.CurrentWindow!;
            windowState.Position = new Vector2(10.0f);

            UIManager manager = UIManager.Instance;
            foreach (BuiltPaintData paintData in manager.VisualManager.BuiltPaints)
            {
                IMGUI.Text($"{paintData.Window!.WindowTitle} (0x{paintData.Window!.WindowId:x4})");
                IMGUI.Indent();
                IMGUI.Text($"Mesh: vtx:{paintData.MeshBuilder.Vertices.Count} idx:{paintData.MeshBuilder.Indices.Count}");
                IMGUI.Text($"Data: {paintData.DataBuffer.Length}b");
                IMGUI.Text($"Segments: {paintData.Segments.Count}");
                IMGUI.Text($"Paint: cmds:{paintData.Data!.Memory.Length}b objs:{paintData.Data!.Objects.Count}");
                IMGUI.Unindent();
            }

            IMGUI.EndWindow();
        }
    }
}
