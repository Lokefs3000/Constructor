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
            UIRoundedCorner cornerRounding = button.CornerRounding;
            UIColor fillColor = button.FillColor;
            UIColor strokeColor = button.StrokeColor;
            UIStrokePosition strokePosition = button.StrokePosition;
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

                    v = Flags.HasFlag(cornerRounding, UIRoundedCorner.TopLeft);
                    if (ImGui.Checkbox("##TL"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, UIRoundedCorner.TopLeft) : Flags.RemoveFlags(cornerRounding, UIRoundedCorner.TopLeft);

                    ImGui.SameLine();

                    v = Flags.HasFlag(cornerRounding, UIRoundedCorner.Top);
                    if (ImGui.Checkbox("##T"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, UIRoundedCorner.Top) : Flags.RemoveFlags(cornerRounding, UIRoundedCorner.Top);

                    ImGui.SameLine();

                    v = Flags.HasFlag(cornerRounding, UIRoundedCorner.TopRight);
                    if (ImGui.Checkbox("##TR"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, UIRoundedCorner.TopRight) : Flags.RemoveFlags(cornerRounding, UIRoundedCorner.TopRight);

                    v = Flags.HasFlag(cornerRounding, UIRoundedCorner.Left);
                    if (ImGui.Checkbox("##L"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, UIRoundedCorner.Left) : Flags.RemoveFlags(cornerRounding, UIRoundedCorner.Left);

                    ImGui.SameLine();

                    v = Flags.HasFlag(cornerRounding, UIRoundedCorner.All);
                    if (ImGui.Checkbox("##ALL"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, UIRoundedCorner.All) : Flags.RemoveFlags(cornerRounding, UIRoundedCorner.All);

                    ImGui.SameLine();

                    v = Flags.HasFlag(cornerRounding, UIRoundedCorner.Right);
                    if (ImGui.Checkbox("##R"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, UIRoundedCorner.Right) : Flags.RemoveFlags(cornerRounding, UIRoundedCorner.Right);

                    v = Flags.HasFlag(cornerRounding, UIRoundedCorner.BottomLeft);
                    if (ImGui.Checkbox("##BL"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, UIRoundedCorner.BottomLeft) : Flags.RemoveFlags(cornerRounding, UIRoundedCorner.BottomLeft);

                    ImGui.SameLine();

                    v = Flags.HasFlag(cornerRounding, UIRoundedCorner.Bottom);
                    if (ImGui.Checkbox("##B"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, UIRoundedCorner.Bottom) : Flags.RemoveFlags(cornerRounding, UIRoundedCorner.Bottom);

                    ImGui.SameLine();

                    v = Flags.HasFlag(cornerRounding, UIRoundedCorner.BottomRight);
                    if (ImGui.Checkbox("##BR"u8, ref v))
                        cornerRounding = v ? Flags.AddFlags(cornerRounding, UIRoundedCorner.BottomRight) : Flags.RemoveFlags(cornerRounding, UIRoundedCorner.BottomRight);

                    if (cornerRounding != button.CornerRounding)
                        button.CornerRounding = cornerRounding;
                }
                ImGui.EndChild();

                ImGui.SameLine();

                ImDrawFlags flags = ImDrawFlags.None;
                if (Flags.HasFlag(cornerRounding, UIRoundedCorner.TopLeft)) flags |= ImDrawFlags.RoundCornersTopLeft;
                if (Flags.HasFlag(cornerRounding, UIRoundedCorner.TopRight)) flags |= ImDrawFlags.RoundCornersTopRight;
                if (Flags.HasFlag(cornerRounding, UIRoundedCorner.BottomLeft)) flags |= ImDrawFlags.RoundCornersBottomLeft;
                if (Flags.HasFlag(cornerRounding, UIRoundedCorner.BottomRight)) flags |= ImDrawFlags.RoundCornersBottomRight;

                Vector2 boxOrigin = ImGui.GetCursorScreenPos() + context.Style.FramePadding;
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                drawList.AddRectFilled(boxOrigin, boxOrigin + new Vector2(boxSize.X), 0xffffffff, cornerRounding == UIRoundedCorner.None ? 0.0f : MathF.Min(cornerRadius == 0.0f ? 16.0f : cornerRadius, boxSize.X * 0.35f), flags);

                ImGui.Dummy(new Vector2(boxSize.X) + context.Style.FramePadding);
                ImGui.SameLine();

                ImGui.Text("Corner rounding"u8);
            }

            if (IElementInspector.InputUIColor("Fill color"u8, ref fillColor))
                button.FillColor = fillColor;

            if (IElementInspector.InputUIColor("Stroke color"u8, ref strokeColor))
                button.StrokeColor = strokeColor;

            if (IElementInspector.ComboBox("Stroke position"u8, ref Unsafe.As<UIStrokePosition, int>(ref strokePosition), s_strokePositionEnum))
                button.StrokePosition = strokePosition;

            if (ImGui.DragFloat("Stroke weight"u8, ref strokeWeight, 1.0f, 0.0f, float.MaxValue))
                button.StrokeWeight = strokeWeight;
        }

        private static readonly string[] s_strokePositionEnum = Enum.GetNames<UIStrokePosition>();
    }
}
