using System;
using System.Collections.Generic;
using System.Text;
using Primary.GUI.ImGui;

namespace EditorUI.Diagnostics.ImGui
{
    public sealed class HierchyExplorer : IImGuiDrawer
    {
        public void Draw()
        {
            if (IMGUI.BeginWindow("Widget hierchy explorer"))
            {
                IMGUI.EndWindow();
            }
        }
    }
}
