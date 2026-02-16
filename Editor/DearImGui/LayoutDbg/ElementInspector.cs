using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Hexa.NET.ImGui;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.DearImGui.LayoutDbg
{
    internal class ElementInspector : IElementInspector
    {
        public void Inspect(UIElement element)
        {
            UIValue2 position = element.Transform.Position;
            UIValue2 size = element.Transform.Size;
            UITransformAlign align = element.Transform.Align;

            if (IElementInspector.InputUIValue2("Position"u8, ref position))
                element.Transform.Position = position;
            if (IElementInspector.InputUIValue2("Size"u8, ref size))
                element.Transform.Size = size;

            {
                ImGui.PushID("TRANSFORM"u8);
                {
                    UITransformAlign narrow = align & (UITransformAlign)0b000_111;

                    if (narrow == UITransformAlign.Left)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("L"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("L"u8))
                        align = (align & (UITransformAlign)0b111_000) | UITransformAlign.Left;

                    ImGui.SameLine();

                    if (narrow == UITransformAlign.Middle)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("M"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("M"u8))
                        align = (align & (UITransformAlign)0b111_000) | UITransformAlign.Middle;

                    ImGui.SameLine();

                    if (narrow == UITransformAlign.Right)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("R"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("R"u8))
                        align = (align & (UITransformAlign)0b111_000) | UITransformAlign.Right;

                    ImGui.SameLine();

                    if (narrow == UITransformAlign.FillHorizontal)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("F##H"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("F##H"u8))
                        align = (align & (UITransformAlign)0b111_000) | UITransformAlign.FillHorizontal;
                }
                {
                    UITransformAlign narrow = align & (UITransformAlign)0b111_000;

                    if (narrow == UITransformAlign.Top)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("T"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("T"u8))
                        align = (align & (UITransformAlign)0b000_111) | UITransformAlign.Top;

                    ImGui.SameLine();

                    if (narrow == UITransformAlign.Center)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("C"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("C"u8))
                        align = (align & (UITransformAlign)0b111_000) | UITransformAlign.Center;

                    ImGui.SameLine();

                    if (narrow == UITransformAlign.Bottom)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("B"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("B"u8))
                        align = (align & (UITransformAlign)0b000_111) | UITransformAlign.Bottom;

                    ImGui.SameLine();

                    if (narrow == UITransformAlign.FillVertical)
                    {
                        ImGui.BeginDisabled();
                        ImGui.Button("F##V"u8);
                        ImGui.EndDisabled();
                    }
                    else if (ImGui.Button("F##V"u8))
                        align = (align & (UITransformAlign)0b000_111) | UITransformAlign.FillVertical;
                }
                ImGui.PopID();

                if (align != element.Transform.Align)
                    element.Transform.Align = align;
            }
        }
    }
}
