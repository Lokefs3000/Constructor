using Hexa.NET.ImGui;
using System.Numerics;

namespace Editor.DearImGui
{
    internal sealed class EditorTaskViewer
    {
        internal EditorTaskViewer()
        {

        }

        internal void Render()
        {
            if (ImGui.Begin("Task viewer"))
            {
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                ImGuiStylePtr style = ImGui.GetStyle();
                ImGuiContextPtr g = ImGui.GetCurrentContext();

                Editor editor = Editor.GlobalSingleton;

                if (editor.AssetPipeline.IsImporting)
                {
                    DrawTaskGui(drawList, style, g, "Importing assets..", editor.AssetPipeline.ImportProgress);
                }
            }
            ImGui.End();
        }

        private void DrawTaskGui(ImDrawListPtr drawList, ImGuiStylePtr style, ImGuiContextPtr g, string title, float progress)
        {
            uint id = ImGui.GetID(title);

            Vector2 screen = ImGui.GetCursorScreenPos();
            Vector2 content = ImGui.GetContentRegionAvail();

            const float BarHeight = 6.0f;

            ImRect rect = new ImRect(screen, screen + new Vector2(content.X, style.FramePadding.Y * 4.0f + g.FontSize + BarHeight));
            ImRect bar = new ImRect(rect.Min + new Vector2(style.FramePadding.X, style.FramePadding.Y * 3.0f + g.FontSize), rect.Min + new Vector2(content.X - style.FramePadding.X, style.FramePadding.Y * 3.0f + g.FontSize + BarHeight));

            drawList.AddRectFilled(rect.Min, rect.Max, 0x1affffff, 3.0f);
            drawList.AddText(rect.Min + style.FramePadding, 0xffffffff, title);
            drawList.AddRectFilled(bar.Min, bar.Max, 0x80000000, BarHeight * 0.5f, ImDrawFlags.RoundCornersAll);
            drawList.AddRectFilled(bar.Min, new Vector2(float.Lerp(bar.Min.X, bar.Max.X, progress), bar.Max.Y), 0x60ffffff, BarHeight * 0.5f, ImDrawFlags.RoundCornersAll);

            ImGuiP.ItemAdd(rect, id);
            ImGuiP.ItemSize(rect, style.FramePadding.Y);
        }
    }
}
