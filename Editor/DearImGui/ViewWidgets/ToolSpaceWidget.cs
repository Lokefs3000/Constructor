using Editor.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui.ViewWidgets
{
    internal sealed class ToolSpaceWidget : SceneViewWidget
    {
        protected override void Render()
        {
            ToolManager tools = Editor.GlobalSingleton.ToolManager;

            int originModeIdx = (int)tools.OriginMode;
            int toolSpaceIdx = (int)tools.ToolSpace;

            if (DrawCombo(1, ref originModeIdx, s_originMode))
                tools.OriginMode = (EditorOriginMode)originModeIdx;
            if (DrawCombo(2, ref toolSpaceIdx, s_toolSpace))
                tools.ToolSpace = (EditorToolSpace)toolSpaceIdx;
        }

        public override ReadOnlySpan<string> RequiredIcons => ReadOnlySpan<string>.Empty;
        public override bool IsFloating => false;

        private static string[] s_originMode = Enum.GetNames<EditorOriginMode>();
        private static string[] s_toolSpace = Enum.GetNames<EditorToolSpace>();
    }
}
