using Editor.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui.ViewWidgets
{
    internal sealed class ToolSelectorWidget : SceneViewWidget
    {
        protected override void Render()
        {
            ToolManager tools = Editor.GlobalSingleton.ToolManager;

            bool translateSelected = tools.Tool == EditorTool.Translate;
            bool rotateSelected = tools.Tool == EditorTool.Rotate;
            bool scaleSelected = tools.Tool == EditorTool.Scale;

            if (DrawToggle(1, ref translateSelected, null, "Editor/Textures/Icons/WgTranslateIcon.png"))
                tools.SwitchTool(EditorTool.Translate);
            DrawTooltip("Translate tool"u8);

            if (DrawToggle(2, ref rotateSelected, null, "Editor/Textures/Icons/WgRotateIcon.png"))
                tools.SwitchTool(EditorTool.Rotate);
            DrawTooltip("Rotate tool"u8);

            if (DrawToggle(3, ref scaleSelected, null, "Editor/Textures/Icons/WgScaleIcon.png"))
                tools.SwitchTool(EditorTool.Scale);
            DrawTooltip("Scale tool"u8);
        }

        public override ReadOnlySpan<string> RequiredIcons => new string[] {
            "Editor/Textures/Icons/WgTranslateIcon.png",
            "Editor/Textures/Icons/WgRotateIcon.png",
        };
    }
}
