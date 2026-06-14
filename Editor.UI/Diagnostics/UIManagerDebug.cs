using Editor.UI.Text;
using Primary.GUI.ImGui;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Diagnostics
{
    internal sealed class UIManagerDebug : IImGuiDrawer
    {
        public void Draw()
        {
            UIManager ui = UIManager.Instance;

            if (IMGUI.BeginWindow("UI manager"))
            {
                IMGUI.Text("Text manager:");
                IMGUI.Indent();
                {
                    TextManager comp = ui.TextManager;

                    if (IMGUI.Button("Text shape cache", comp.TextShapeCache.IsEnabled))
                        comp.TextShapeCache.IsEnabled = !comp.TextShapeCache.IsEnabled;
                }
                IMGUI.Unindent();

                IMGUI.EndWindow();
            }
        }
    }
}
