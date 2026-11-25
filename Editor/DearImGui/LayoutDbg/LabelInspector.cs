using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Hexa.NET.ImGui;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Editor.DearImGui.LayoutDbg
{
    internal sealed class LabelInspector : IElementInspector
    {
        public void Inspect(UIElement element)
        {
            UILabel label = Unsafe.As<UILabel>(element);

            string text = label.Text;
            float size = label.FontSize;

            UITextAlignment alignment = label.Alignment;
            UITextOverflow overflow = label.Overflow;

            bool autoSize = label.AutoSize == UITextAutoSize.FitBoundsToText;
            
            UIColor fillColor = label.TextColor;

            if (ImGui.InputTextMultiline("Text"u8, ref text, ushort.MaxValue))
                label.Text = text;
            if (ImGui.DragFloat("Size"u8, ref size, 0.02f, 0.0f, float.MaxValue))
                label.FontSize = size;

            {
                UITextAlignment prev = alignment;
                Vector2 sc = ImGui.GetCursorScreenPos();
                {
                    UITextAlignment align = alignment & UITextAlignment.LCRMask;

                    if (align == UITextAlignment.Left)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("L"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("L"u8))
                        alignment = (UITextAlignment)(((int)alignment & ~0b111) | (int)UITextAlignment.Left);

                    ImGui.SameLine();

                    if (align == UITextAlignment.Center)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("C"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("C"u8))
                        alignment = (UITextAlignment)(((int)alignment & ~0b111) | (int)UITextAlignment.Center);

                    ImGui.SameLine();

                    if (align == UITextAlignment.Right)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("R"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("R"u8))
                        alignment = (UITextAlignment)(((int)alignment & ~0b111) | (int)UITextAlignment.Right);
                }
                {
                    UITextAlignment align = alignment & UITextAlignment.TMBMask;

                    if (align == UITextAlignment.Top)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("T"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("T"u8))
                        alignment = (UITextAlignment)(((int)alignment & ~0b111_000) | (int)UITextAlignment.Top);

                    ImGui.SameLine();

                    if (align == UITextAlignment.Middle)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("M"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("M"u8))
                        alignment = (UITextAlignment)(((int)alignment & ~0b111_000) | (int)UITextAlignment.Middle);

                    ImGui.SameLine();

                    if (align == UITextAlignment.Bottom)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("B"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("B"u8))
                        alignment = (UITextAlignment)(((int)alignment & ~0b111_000) | (int)UITextAlignment.Bottom);
                }
                if (prev != alignment)
                    label.Alignment = alignment;

                float height = ImGui.GetCursorScreenPos().Y - sc.Y;

                ImGuiContextPtr context = ImGui.GetCurrentContext();
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                Vector2 textPos = sc + new Vector2(150.0f, height * 0.5f);
                Vector2 textSize = ImGui.CalcTextSize("Lorem ipsum!"u8);

                drawList.AddLine(textPos, textPos + new Vector2(150.0f, 0.0f), 0x80ffffff);

                switch (alignment & UITextAlignment.LCRMask)
                {
                    case UITextAlignment.Left: break;
                    case UITextAlignment.Center: textPos.X += 150.0f * 0.5f - textSize.Y * 0.5f; break;
                    case UITextAlignment.Right: textPos.X += 150.0f - textSize.X; break;
                }

                switch (alignment & UITextAlignment.TMBMask)
                {
                    case UITextAlignment.Top: textPos.Y -= context.FontSize; break;
                    case UITextAlignment.Middle: textPos.Y -= textSize.Y * 0.5f; break;
                    case UITextAlignment.Bottom: break;
                }

                drawList.AddText(textPos, 0xffffffff, "Lorem ipsum!"u8);
            }
            if (IElementInspector.ComboBox("Overflow"u8, ref Unsafe.As<UITextOverflow, int>(ref overflow), s_textOverflowEnum))
                label.Overflow = overflow;

            if (ImGui.Checkbox("Auto size"u8, ref autoSize))
                label.AutoSize = autoSize ? UITextAutoSize.FitBoundsToText : UITextAutoSize.None;

            if (IElementInspector.InputUIColor("Text color"u8, ref fillColor))
                label.TextColor = fillColor;
        }

        private static readonly string[] s_textOverflowEnum = Enum.GetNames<UITextOverflow>();
        private static readonly string[] s_textAutoSizeEnum = Enum.GetNames<UITextAutoSize>();
    }
}
