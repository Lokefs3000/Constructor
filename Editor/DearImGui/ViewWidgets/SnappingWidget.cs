using Editor.Interaction;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui.ViewWidgets
{
    internal class SnappingWidget : SceneViewWidget
    {

        protected override void Render()
        {
            bool value = ToolManager.IsSnappingDefault;
            if (DrawToggle(1, ref value, null, "Editor/Textures/Icons/WgGridSnapIcon.png"))
                ToolManager.IsSnappingDefault = value;

            float value2 = ToolManager.SnapScale;
            ImGui.SetNextItemWidth(CalculateWidgetSize().X * 2.0f);
            if (ImGui.InputFloat("##2"u8, ref value2, "%g"u8))
                ToolManager.SnapScale = value2;
        }

        public override ReadOnlySpan<string> RequiredIcons => new string[] {
            "Editor/Textures/Icons/WgGridSnapIcon.png",
        };
    }
}
