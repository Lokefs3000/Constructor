using Primary.GUI.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Gui.Debugging
{
    internal sealed class LayoutDebugger : IImGuiDrawer
    {
        public void Draw()
        {
            if (IMGUI.BeginWindow("Layout debugger"))
            {
                if (IMGUI.BeginMenuBar())
                {

                    IMGUI.EndMenuBar();
                }

                Vector2 avail = Vector2.Max(IMGUI.AvailableSize, new Vector2(150.0f));

                float lowerHeight = MathF.Min(avail.Y * 0.4f, 200.0f);

                if (IMGUI.BeginChild(1, new Vector2(avail.X, avail.Y - lowerHeight), ImGuiChildFlags.Borders))
                {
                    IMGUI.Button("Select window");
                    IMGUI.EndChild();
                }

                if (IMGUI.BeginChild(2, new Vector2(avail.X, lowerHeight), ImGuiChildFlags.Borders))
                {
                    IMGUI.EndChild();
                }
            }
            IMGUI.EndWindow();
        }
    }
}
