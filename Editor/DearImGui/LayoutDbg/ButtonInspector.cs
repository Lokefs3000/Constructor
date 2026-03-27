using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Visual;
using Hexa.NET.ImGui;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.DearImGui.LayoutDbg
{
    internal sealed class ButtonInspector : IElementInspector
    {
        public void Inspect(UIElement element)
        {
            UIButton button = Unsafe.As<UIButton>(element);

            float cornerRadius = button.CornerRadius;
            RectCorner cornerRounding = button.Corners;
            UIColor fillColor = button.BackgroundColor;
            UIColor strokeColor = button.StrokeColor;
            StrokePosition strokePosition = button.StrokePosition;
            float strokeWeight = button.StrokeWeight;

            if (ImGui.DragFloat("Corner radius"u8, ref cornerRadius, 1.0f, 0.0f, float.MaxValue))
                button.CornerRadius = cornerRadius;

            {
                ImGuiContextPtr context = ImGui.GetCurrentContext();

                Vector2 boxSize = Vector2.Zero;
                if (ImGui.BeginChild("Corner rounding"u8, ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeX | ImGuiChildFlags.AutoResizeY))
                {
                    bool v = false;

                    boxSize = ImGui.GetContentRegionAvail();

                    v = Flags.HasFlag(cornerRounding, RectCorner.TopLeft);
                    if (ImGui.Checkbox("##TL"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, RectCorner.TopLeft) : Flags.RemoveFlags(cornerRounding, RectCorner.TopLeft);

                    ImGui.SameLine();

                    v = Flags.HasFlag(cornerRounding, RectCorner.Top);
                    if (ImGui.Checkbox("##T"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, RectCorner.Top) : Flags.RemoveFlags(cornerRounding, RectCorner.Top);

                    ImGui.SameLine();

                    v = Flags.HasFlag(cornerRounding, RectCorner.TopRight);
                    if (ImGui.Checkbox("##TR"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, RectCorner.TopRight) : Flags.RemoveFlags(cornerRounding, RectCorner.TopRight);

                    v = Flags.HasFlag(cornerRounding, RectCorner.Left);
                    if (ImGui.Checkbox("##L"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, RectCorner.Left) : Flags.RemoveFlags(cornerRounding, RectCorner.Left);

                    ImGui.SameLine();

                    v = Flags.HasFlag(cornerRounding, RectCorner.All);
                    if (ImGui.Checkbox("##ALL"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, RectCorner.All) : Flags.RemoveFlags(cornerRounding, RectCorner.All);

                    ImGui.SameLine();

                    v = Flags.HasFlag(cornerRounding, RectCorner.Right);
                    if (ImGui.Checkbox("##R"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, RectCorner.Right) : Flags.RemoveFlags(cornerRounding, RectCorner.Right);

                    v = Flags.HasFlag(cornerRounding, RectCorner.BottomLeft);
                    if (ImGui.Checkbox("##BL"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, RectCorner.BottomLeft) : Flags.RemoveFlags(cornerRounding, RectCorner.BottomLeft);

                    ImGui.SameLine();

                    v = Flags.HasFlag(cornerRounding, RectCorner.Bottom);
                    if (ImGui.Checkbox("##B"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, RectCorner.Bottom) : Flags.RemoveFlags(cornerRounding, RectCorner.Bottom);

                    ImGui.SameLine();

                    v = Flags.HasFlag(cornerRounding, RectCorner.BottomRight);
                    if (ImGui.Checkbox("##BR"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, RectCorner.BottomRight) : Flags.RemoveFlags(cornerRounding, RectCorner.BottomRight);

                    if (cornerRounding != button.Corners)
                        button.Corners = cornerRounding;
                }
                ImGui.EndChild();

                ImGui.SameLine();

                ImDrawFlags flags = ImDrawFlags.None;
                if (Flags.HasFlag(cornerRounding, RectCorner.TopLeft)) flags |= ImDrawFlags.RoundCornersTopLeft;
                if (Flags.HasFlag(cornerRounding, RectCorner.TopRight)) flags |= ImDrawFlags.RoundCornersTopRight;
                if (Flags.HasFlag(cornerRounding, RectCorner.BottomLeft)) flags |= ImDrawFlags.RoundCornersBottomLeft;
                if (Flags.HasFlag(cornerRounding, RectCorner.BottomRight)) flags |= ImDrawFlags.RoundCornersBottomRight;

                Vector2 boxOrigin = ImGui.GetCursorScreenPos() + context.Style.FramePadding;
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                drawList.AddRectFilled(boxOrigin, boxOrigin + new Vector2(boxSize.X), 0xffffffff, cornerRounding == RectCorner.None ? 0.0f : MathF.Min(cornerRadius == 0.0f ? 16.0f : cornerRadius, boxSize.X * 0.35f), flags);

                ImGui.Dummy(new Vector2(boxSize.X) + context.Style.FramePadding);
                ImGui.SameLine();

                ImGui.Text("Corner rounding"u8);
            }

            if (IElementInspector.InputUIColor("Fill color"u8, ref fillColor))
                button.BackgroundColor = fillColor;

            if (IElementInspector.InputUIColor("Stroke color"u8, ref strokeColor))
                button.StrokeColor = strokeColor;

            if (IElementInspector.ComboBox("Stroke position"u8, ref Unsafe.As<StrokePosition, int>(ref strokePosition), s_strokePositionEnum))
                button.StrokePosition = strokePosition;

            if (ImGui.DragFloat("Stroke weight"u8, ref strokeWeight, 1.0f, 0.0f, float.MaxValue))
                button.StrokeWeight = strokeWeight;
        }

        private static readonly string[] s_strokePositionEnum = Enum.GetNames<StrokePosition>();
    }
}
