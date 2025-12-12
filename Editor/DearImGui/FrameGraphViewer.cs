using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.DearImGui
{
    internal sealed class FrameGraphViewer : IDearImGuiWindow
    {
        public FrameGraphViewer()
        {

        }

        public void Render()
        {
            if (ImGui.Begin("Frame graph viewer"u8, ImGuiWindowFlags.MenuBar))
            {
                if (ImGui.BeginMenuBar())
                {

                }
                ImGui.EndMenuBar();
            }
            ImGui.End();
        }
    }
}
