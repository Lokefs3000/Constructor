using Editor.Interaction;
using Editor.Interaction.Controls;
using Editor.Interaction.Tools;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui.Debuggers
{
    internal class ToolDebugger : IVisualDebugger
    {
        public void Render()
        {
            ToolManager tools = Editor.GlobalSingleton.ToolManager;

            Vector2 availSize = ImGui.GetContentRegionAvail();

            ImGui.BeginTable("##INFO"u8, 2);
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted("Active control:"u8);
            ImGui.TextUnformatted("Active tool:"u8);
            ImGui.TextUnformatted("Origin mode:"u8);
            ImGui.TextUnformatted("Tool space:"u8);

            ImGui.TableSetColumnIndex(1);
            ImGui.Selectable(tools.ActiveControlToolType.ToString()); ImGui.OpenPopupOnItemClick("##CH_CT", ImGuiPopupFlags.MouseButtonLeft);
            ImGui.Selectable(tools.Tool.ToString()); ImGui.OpenPopupOnItemClick("##CH_TL", ImGuiPopupFlags.MouseButtonLeft);
            ImGui.Selectable(tools.OriginMode.ToString()); ImGui.OpenPopupOnItemClick("##CH_OM", ImGuiPopupFlags.MouseButtonLeft);
            ImGui.Selectable(tools.ToolSpace.ToString()); ImGui.OpenPopupOnItemClick("##CH_TS", ImGuiPopupFlags.MouseButtonLeft);

            if (ImGui.BeginPopup("##CH_CT"u8, ImGuiWindowFlags.NoMove))
            {
                if (ImGui.Selectable("Generic"u8, tools.ActiveControlToolType == EditorControlTool.Generic)) tools.SwitchControl(EditorControlTool.Generic);
                if (ImGui.Selectable("GeoEdit"u8, tools.ActiveControlToolType == EditorControlTool.GeoEdit)) tools.SwitchControl(EditorControlTool.GeoEdit);

                ImGui.EndPopup();
            }

            if (ImGui.BeginPopup("##CH_TL"u8, ImGuiWindowFlags.NoMove))
            {
                if (ImGui.Selectable("Translate"u8, tools.Tool == EditorTool.Translate)) tools.SwitchTool(EditorTool.Translate);
                if (ImGui.Selectable("Rotate"u8, tools.Tool == EditorTool.Rotate)) tools.SwitchTool(EditorTool.Rotate);
                if (ImGui.Selectable("Scale"u8, tools.Tool == EditorTool.Scale)) tools.SwitchTool(EditorTool.Scale);

                ImGui.EndPopup();
            }

            if (ImGui.BeginPopup("##CH_OM"u8, ImGuiWindowFlags.NoMove))
            {
                if (ImGui.Selectable("Individual"u8, tools.OriginMode == EditorOriginMode.Individual)) tools.OriginMode = EditorOriginMode.Individual;
                if (ImGui.Selectable("Center"u8, tools.OriginMode == EditorOriginMode.Center)) tools.OriginMode = EditorOriginMode.Center;

                ImGui.EndPopup();
            }

            if (ImGui.BeginPopup("##CH_TS"u8, ImGuiWindowFlags.NoMove))
            {
                if (ImGui.Selectable("Local"u8, tools.ToolSpace == EditorToolSpace.Local)) tools.ToolSpace = EditorToolSpace.Local;
                if (ImGui.Selectable("Global"u8, tools.ToolSpace == EditorToolSpace.Global)) tools.ToolSpace = EditorToolSpace.Global;

                ImGui.EndPopup();
            }

            ImGui.EndTable();

            if (ImGui.CollapsingHeader("Current control"u8))
            {
                IControlTool control = tools.ActiveControlTool;
                ReadOnlySpan<IToolTransform> transforms = control.Transforms;

                for (int i = 0; i < transforms.Length; i++)
                {
                    ref readonly IToolTransform transform = ref transforms[i];
                    if (transform is EntityToolTransform ett)
                        ImGui.BulletText($"EntityToolTransform {{ \"{ett.Entity}\" }}");
                    else if (transform is GeoBrushToolTransform gbtt)
                        ImGui.BulletText($"GeoBrushToolTransform {{ \"{gbtt.Brush}\" }}");
                    else
                        ImGui.BulletText(transform.ToString());
                }
            }

            if (ImGui.CollapsingHeader("Current tool"u8))
            {

            }
        }

        public VisualDebuggerType DebuggerType => VisualDebuggerType.Editor;
        public ReadOnlySpan<byte> DebuggerName => "Tools"u8;
    }
}
