using Editor.Gui;
using Hexa.NET.ImGui;
using Primary.Common;
using System.Numerics;

namespace Editor.DearImGui.ViewWidgets
{
    internal abstract class SceneViewWidget
    {
        private DynamicIconSet? _iconSet;

        internal void SetIconSet(DynamicIconSet iconSet)
        {
            _iconSet = iconSet;
        }

        internal void RenderSelf()
        {
            if (IsFloating)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6.0f));
                ImGui.PushStyleColor(ImGuiCol.WindowBg, 0x80303030);
                ImGui.PushStyleColor(ImGuiCol.Border, 0xaa606060);

                bool windowRet = ImGui.Begin(GetType().Name, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDocking);

                ImGui.PopStyleVar(2);
                ImGui.PopStyleColor(2);

                if (windowRet)
                {
                    Render();
                }
                ImGui.End();
            }
            else
            {
                ImGuiContextPtr context = ImGui.GetCurrentContext();
                if (ImGui.BeginChild(GetType().Name, ImGuiChildFlags.AutoResizeX))
                {
                    {
                        ref ImGuiWindowPtr window = ref context.CurrentWindow;

                        uint id = ImGui.GetID(3000);
                        ImRect bounds = new ImRect(window.DC.CursorPos, window.DC.CursorPos + new Vector2(6.0f, window.DC.CursorMaxPos.Y));

                        bool hovered = false, held = false;
                        ImGuiP.ButtonBehavior(bounds, id, ref hovered, ref held);

                        ImGuiCol col = held ? ImGuiCol.ButtonActive : (hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button);
                        uint abgr = new Color32(context.Style.Colors[(int)col]).ABGR;

                        window.DrawList.AddRectFilled(new Vector2(bounds.Min.X, bounds.Min.Y + 2.0f), new Vector2(bounds.Min.X + 1.0f, bounds.Max.Y - 2.0f), abgr);
                        window.DrawList.AddRectFilled(new Vector2(bounds.Max.X - 1.0f, bounds.Min.Y + 2.0f), new Vector2(bounds.Max.X, bounds.Max.Y - 2.0f), abgr);

                        ImGuiP.ItemAdd(bounds, id);
                        ImGuiP.ItemSize(bounds);

                        ImGui.SameLine();
                    }

                    Render();
                    ImGui.EndChild();
                }
            }
        }

        protected abstract void Render();
        public abstract bool IsFloating { get; }

        protected static float CalculateMetrics()
        {
            ImGuiContextPtr context = ImGui.GetCurrentContext();
            return 0.0f;
        }

        protected static Vector2 CalculateWidgetSize()
        {
            ImGuiContextPtr context = ImGui.GetCurrentContext();
            return new Vector2(context.FontSize) + new Vector2(context.Style.FramePadding.Y) * 2.0f;
        }

        protected bool DrawCombo(int id, ref int index, ReadOnlySpan<string> options)
        {
            bool ret = false;

            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ref ImGuiWindowPtr window = ref context.CurrentWindow;

            ImGuiNextWindowDataFlags backupFlags = context.NextWindowData.HasFlags;
            ImGuiP.ClearFlags(ref context.NextWindowData);

            uint idVal = ImGui.GetID(id);

            Vector2 textSize = ImGui.CalcTextSize(options[index]);
            float arrowWidth = 8.0f;
            ImRect bounds = new ImRect(window.DC.CursorPos, window.DC.CursorPos + textSize + context.Style.FramePadding * 2.0f + new Vector2(arrowWidth + context.Style.FramePadding.X, 0.0f));

            bool hovered = false, held = false;
            bool pressed = ImGuiP.ButtonBehavior(bounds, idVal, ref hovered, ref held);

            uint popupId = ImGuiP.ImHashStr("##ComboPopup"u8, 0, idVal);
            bool popupOpen = ImGuiP.IsPopupOpen(popupId, ImGuiPopupFlags.None);

            if (pressed && !popupOpen)
            {
                ImGuiP.OpenPopupEx(popupId, ImGuiPopupFlags.None);
                popupOpen = true;
            }

            if (hovered && context.IO.MouseWheel != 0.0f)
            {
                if (context.IO.MouseWheel > 0.0f)
                    index = index + 1 >= options.Length ? 0 : index + 1;
                else
                    index = index - 1 < 0 ? options.Length - 1 : index - 1;

                ret = true;
            }

            ImGuiP.ItemAdd(bounds, idVal);
            ImGuiP.ItemSize(bounds);

            ImGui.SameLine();

            Vector4 bodyColor = context.Style.Colors[(int)(held ? ImGuiCol.FrameBgActive : (hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg))];

            window.DrawList.AddRectFilled(bounds.Min, bounds.Max, new Color32(bodyColor).ABGR, context.Style.FrameRounding);
            if (popupOpen)
                window.DrawList.AddRect(bounds.Min, bounds.Max, 0xff2070a0, context.Style.FrameRounding);

            window.DrawList.AddText(bounds.Min + context.Style.FramePadding, 0xffffffff, options[index]);

            Vector2 arrowCenter = new Vector2(bounds.Min.X + textSize.X + context.Style.FramePadding.X * 2.0f + arrowWidth * 0.5f, float.Lerp(bounds.Min.Y, bounds.Max.Y, 0.5f)) + new Vector2(-1.0f, 1.0f);
            if (popupOpen)
                window.DrawList.AddTriangleFilled(arrowCenter - new Vector2(4.0f, -3.0f), arrowCenter + new Vector2(0.0f, -3.0f), arrowCenter + new Vector2(4.0f, 3.0f), new Color32(context.Style.Colors[(int)ImGuiCol.FrameBgActive] * 1.1f).ABGR);
            else
                window.DrawList.AddTriangleFilled(arrowCenter - new Vector2(4.0f, 3.0f), arrowCenter + new Vector2(0.0f, 3.0f), arrowCenter + new Vector2(4.0f, -3.0f), new Color32(context.Style.Colors[(int)ImGuiCol.FrameBgActive] * 1.1f).ABGR);

            if (!popupOpen)
                return ret;

            context.NextWindowData.HasFlags = backupFlags;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(3.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, context.Style.FrameRounding);
            if (ImGuiP.BeginComboPopup(popupId, bounds, ImGuiComboFlags.None))
            {
                for (int i = 0; i < options.Length; i++)
                {
                    if (ImGui.Selectable(options[i], i == index))
                    {
                        index = i;
                        ret = true;
                    }
                }

                ImGui.EndCombo();
            }
            ImGui.PopStyleVar(2);

            return ret;
        }

        protected bool DrawToggle(int id, ref bool toggled, ReadOnlySpan<byte> flavor, in string? icon = null)
        {
            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ref ImGuiWindowPtr window = ref context.CurrentWindow;

            uint idVal = ImGui.GetID(id);

            Vector2 textSize = flavor.IsEmpty ? new Vector2(context.FontSize) : ImGui.CalcTextSize(flavor);
            ImRect bounds = new ImRect(window.DC.CursorPos, window.DC.CursorPos + textSize + new Vector2(context.Style.FramePadding.Y) * 2.0f);

            bool hovered = false, held = false;
            bool pressed = ImGuiP.ButtonBehavior(bounds, idVal, ref hovered, ref held);

            if (pressed)
                toggled = !toggled;

            ImGuiP.ItemAdd(bounds, idVal);
            ImGuiP.ItemSize(bounds);

            ImGui.SameLine();

            Vector4 bodyColor = context.Style.Colors[(int)(held ? ImGuiCol.FrameBgActive : (hovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg))];

            if (toggled)
                window.DrawList.AddRect(bounds.Min, bounds.Max, 0xff2070a0, context.Style.FrameRounding);
            window.DrawList.AddRectFilled(bounds.Min, bounds.Max, new Color32(bodyColor).ABGR, context.Style.FrameRounding);

            if (icon != null)
            {
                if (_iconSet != null && _iconSet.TryGetAtlasIcon(icon, out DynAtlasIcon data))
                    window.DrawList.AddImage(ImGuiUtility.GetTextureRef(_iconSet.AtlasTexture.Handle), bounds.Min + new Vector2(2.0f), bounds.Max - new Vector2(2.0f), data.UVs.Minimum, data.UVs.Maximum, 0xffffffff);
                else
                    window.DrawList.AddRectFilled(bounds.Min + new Vector2(2.0f), bounds.Max - new Vector2(2.0f), 0xffff00ff);
            }

            if (!flavor.IsEmpty)
                window.DrawList.AddText(bounds.Min + context.Style.FramePadding, 0xffffffff, flavor);

            return pressed;
        }

        protected static void DrawTooltip(ReadOnlySpan<byte> text)
        {
            if (!text.IsEmpty)
            {
                if (ImGui.BeginItemTooltip())
                {
                    ImGui.TextUnformatted(text);
                    ImGui.EndTooltip();
                }
            }
        }

        public abstract ReadOnlySpan<string> RequiredIcons { get; }
    }
}
