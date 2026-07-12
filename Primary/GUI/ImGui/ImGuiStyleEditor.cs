using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Primary.GUI.ImGui
{
    internal static class ImGuiStyleEditor
    {
        internal static void Show()
        {
            if (IMGUI.BeginWindow("Style editor"))
            {
                ImGuiStyle style = IMGUI.Style;

                // vector2
                {
                    Vector2 v;

                    v = style.FramePadding;
                    if (IMGUI.DragVector2("Frame padding:", ref v))
                        style.FramePadding = v;

                    v = style.WindowPadding;
                    if (IMGUI.DragVector2("Window padding:", ref v))
                        style.WindowPadding = v;

                    v = style.ItemPadding;
                    if (IMGUI.DragVector2("Item padding:", ref v))
                        style.ItemPadding = v;

                    v = style.InnerItemPadding;
                    if (IMGUI.DragVector2("Inner item padding:", ref v))
                        style.InnerItemPadding = v;
                }

                IMGUI.EndWindow();
            }
        }
    }
}
