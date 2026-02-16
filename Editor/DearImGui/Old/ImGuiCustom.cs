using Hexa.NET.ImGui;
using System.Numerics;

namespace Editor.DearImGui
{
    internal static unsafe class ImGuiCustom
    {
        public static bool RangeSliderScalar(ReadOnlySpan<byte> label, ImGuiDataType data_type, void* p_dataMin, void* p_dataMax, void* p_rangeMin, void* p_rangeMax, bool fSlideBlock, byte* formatMin, byte* formatMax, ImGuiSliderFlags flags)
        {
            ImGuiWindowPtr window = ImGuiP.GetCurrentWindow();
            if (window.SkipItems)
                return false;

            ImGuiContextPtr g = ImGui.GetCurrentContext();
            ref ImGuiStyle style = ref g.Style;
            float w = ImGui.CalcItemWidth();

            Vector2 label_size = ImGui.CalcTextSize(label, (byte*)null, true);
            ImRect frame_bb = new ImRect(window.DC.CursorPos, window.DC.CursorPos + new Vector2(w, label_size.Y + style.FramePadding.Y * 2.0f));
            ImRect total_bb = new ImRect(frame_bb.Min, frame_bb.Max + new Vector2(label_size.X > 0.0f ? style.ItemInnerSpacing.X + label_size.X : 0.0f, 0.0f));

            ImGuiP.ItemSize(total_bb, style.FramePadding.Y);

            // Default format string when passing NULL
            if (formatMin == null)
                formatMin = ImGuiP.DataTypeGetInfo(data_type).PrintFmt;
            if (formatMax == null)
                formatMax = ImGuiP.DataTypeGetInfo(data_type).PrintFmt;

            uint idMin = ImGui.GetID("Min");
            ImRect frame_bb_Min = new ImRect(frame_bb.Min + new Vector2(0.0f, (frame_bb.Max.Y - frame_bb.Min.Y) / 2.0f), frame_bb.Min + (frame_bb.Max - frame_bb.Min));

            if (!ImGuiP.ItemAdd(frame_bb_Min, idMin, &frame_bb_Min))
                return false;

            uint idMax = ImGui.GetID("Max");
            ImRect frame_bb_Max = new ImRect(frame_bb.Min, frame_bb.Min + new Vector2((frame_bb.Max.X - frame_bb.Min.X), (frame_bb.Max.Y - frame_bb.Min.Y) / 2.0f));

            if (!ImGuiP.ItemAdd(frame_bb_Max, idMax, &frame_bb_Max))
                return false;

            // Tabbing or CTRL-clicking on either Slider turns it into an input box
            bool temp_input_allowed = (flags & ImGuiSliderFlags.NoInput) == 0;
            bool temp_input_is_active_min = temp_input_allowed && ImGuiP.TempInputIsActive(idMin);
            bool temp_input_is_active_max = temp_input_allowed && ImGuiP.TempInputIsActive(idMax);

            bool hoveredMin = !temp_input_is_active_max && ImGuiP.ItemHoverable(frame_bb_Min, idMin, ImGuiItemFlags.None);
            bool clickedMin = (hoveredMin && g.IO.MouseClicked_0);
            if (clickedMin || g.NavActivateId == idMin)
            {
                ImGuiP.SetActiveID(idMin, window);
                ImGuiP.SetFocusID(idMin, window);
                ImGuiP.FocusWindow(window);
                g.ActiveIdUsingNavDirMask |= (1 << (int)ImGuiDir.Left) | (1 << (int)ImGuiDir.Right);
                if (temp_input_allowed && ((clickedMin && g.IO.KeyCtrl > 0)))
                {
                    temp_input_is_active_min = true;
                }
            }

            if (temp_input_is_active_min)
            {
                //NOTE: We always block going past the other value when typing. Otherwise in the process of typing, the other value would likely get moved, even if the final entered value is within range.
                // Ideally, we would only apply the changed value when the TempInputScalar loses focus or the user hits enter but there's no easy way to do this.
                bool fBlock = true;
                // Only clamp CTRL+Click input when ImGuiSliderFlags_AlwaysClamp is set
                bool clamp_to_min = (flags & ImGuiSliderFlags.AlwaysClamp) != 0;
                bool clamp_to_max = ((flags & ImGuiSliderFlags.AlwaysClamp) != 0) || fBlock;
                void* p_clampMax = fBlock ? p_dataMax : p_rangeMax;
                return ImGuiP.TempInputScalar(frame_bb, idMin, "Min", data_type, p_dataMin, formatMin, clamp_to_min ? p_rangeMin : null, clamp_to_max ? p_clampMax : null);
            }

            bool hoveredMax = !temp_input_is_active_max && ImGuiP.ItemHoverable(frame_bb_Max, idMax, ImGuiItemFlags.None);
            bool clickedMax = (hoveredMax && g.IO.MouseClicked_0);
            if (clickedMax || g.NavActivateId == idMax)
            {
                ImGuiP.SetActiveID(idMax, window);
                ImGuiP.SetFocusID(idMax, window);
                ImGuiP.FocusWindow(window);
                g.ActiveIdUsingNavDirMask |= (1 << (int)ImGuiDir.Left) | (1 << (int)ImGuiDir.Right);
                if (temp_input_allowed && ((clickedMax && g.IO.KeyCtrl > 0)))
                {
                    temp_input_is_active_max = true;
                }
            }

            if (temp_input_is_active_max)
            {
                //NOTE: We always block going past the other value when typing. Otherwise in the process of typing, the other value would likely get moved, even if the final entered value is within range.
                // Ideally, we would only apply the changed value when the TempInputScalar loses focus or the user hits enter but there's no easy way to do this.
                bool fBlock = true;
                // Only clamp CTRL+Click input when ImGuiSliderFlags_AlwaysClamp is set
                bool clamp_to_min = ((flags & ImGuiSliderFlags.AlwaysClamp) != 0) || fBlock;
                bool clamp_to_max = (flags & ImGuiSliderFlags.AlwaysClamp) != 0;
                void* p_clampMin = fBlock ? p_dataMin : p_rangeMin;
                return ImGuiP.TempInputScalar(frame_bb, idMax, "Max", data_type, p_dataMax, formatMax, clamp_to_min ? p_clampMin : null, clamp_to_max ? p_rangeMax : null);
            }


            // Slider behavior
            ImRect grab_bb_Min;
            bool minChanged = ImGuiP.SliderBehavior(frame_bb, idMin, data_type, p_dataMin, p_rangeMin, p_rangeMax, formatMin, flags, &grab_bb_Min);
            ImRect grab_bb_Max;
            bool maxChanged = ImGuiP.SliderBehavior(frame_bb, idMax, data_type, p_dataMax, p_rangeMin, p_rangeMax, formatMax, flags, &grab_bb_Max);

            //make sure the min never goes over the max
            if (minChanged)
            {
                if (ImGuiP.DataTypeCompare(data_type, p_dataMin, p_dataMax) == 1)
                {
                    if (fSlideBlock)
                    {
                        ImGuiP.DataTypeClamp(data_type, p_dataMin, p_rangeMin, p_dataMax);
                        grab_bb_Min = grab_bb_Max;
                    }
                    else
                    {
                        ImGuiP.DataTypeClamp(data_type, p_dataMax, p_dataMin, p_rangeMax);
                        grab_bb_Max = grab_bb_Min;
                    }
                }
                ImGuiP.MarkItemEdited(idMin);
            }
            if (maxChanged)
            {
                if (ImGuiP.DataTypeCompare(data_type, p_dataMin, p_dataMax) == 1)
                {
                    if (fSlideBlock)
                    {
                        ImGuiP.DataTypeClamp(data_type, p_dataMax, p_dataMin, p_rangeMax);
                        grab_bb_Max = grab_bb_Min;
                    }
                    else
                    {
                        ImGuiP.DataTypeClamp(data_type, p_dataMin, p_rangeMin, p_dataMax);
                        grab_bb_Min = grab_bb_Max;
                    }
                }
                ImGuiP.MarkItemEdited(idMax);
            }

            //Draw each half of the frame as needed
            uint frame_col_Min = ImGui.GetColorU32(g.ActiveId == idMin ? ImGuiCol.FrameBgActive : g.HoveredId == idMin ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg);
            ImGui.PushClipRect(frame_bb_Min.Min, frame_bb_Min.Max, true);
            ImGuiP.RenderNavCursor(frame_bb, idMin);
            ImGuiP.RenderFrame(frame_bb.Min, frame_bb.Max, frame_col_Min, true, g.Style.FrameRounding);

            //draw the other half of the Max handle while we have the clip set up, we draw it first so the min handle can cover it if they overlap
            window.DrawList.AddRectFilled(grab_bb_Max.Min, grab_bb_Max.Max, ImGui.GetColorU32(ImGuiCol.SliderGrab, 0.5f), style.GrabRounding);

            // Render grab
            if (grab_bb_Min.Max.X > grab_bb_Min.Min.X)
            {
                window.DrawList.AddRectFilled(grab_bb_Min.Min, grab_bb_Min.Max, frame_col_Min, style.GrabRounding); //The handle color is alpha so we need to clear the space first in case the other half of the max handle is below us.
                window.DrawList.AddRectFilled(grab_bb_Min.Min, grab_bb_Min.Max, ImGui.GetColorU32(g.ActiveId == idMin ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab), style.GrabRounding);
            }
            ImGui.PopClipRect();

            uint frame_col_Max = ImGui.GetColorU32(g.ActiveId == idMax ? ImGuiCol.FrameBgActive : g.HoveredId == idMax ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg);
            ImGui.PushClipRect(frame_bb_Max.Min, frame_bb_Max.Max, true);
            ImGuiP.RenderNavCursor(frame_bb, idMax);
            ImGuiP.RenderFrame(frame_bb.Min, frame_bb.Max, frame_col_Max, true, g.Style.FrameRounding);

            //draw the other half of the Min handle while we have the clip set up, we draw it first so the max handle can cover it if they overlap
            window.DrawList.AddRectFilled(grab_bb_Min.Min, grab_bb_Min.Max, ImGui.GetColorU32(ImGuiCol.SliderGrab, 0.5f), style.GrabRounding);

            // Render grab
            if (grab_bb_Max.Max.X > grab_bb_Max.Min.X)
            {
                window.DrawList.AddRectFilled(grab_bb_Max.Min, grab_bb_Max.Max, frame_col_Max, style.GrabRounding); //The handle color is alpha so we need to clear the space first in case the other half of the min handle is below us.
                window.DrawList.AddRectFilled(grab_bb_Max.Min, grab_bb_Max.Max, ImGui.GetColorU32(g.ActiveId == idMax ? ImGuiCol.SliderGrabActive : ImGuiCol.SliderGrab), style.GrabRounding);
            }
            ImGui.PopClipRect();

            //Draw a line covering the range
            float lineAlpha = 0.25f;
            float centerHeight = float.Lerp(grab_bb_Min.Min.Y, grab_bb_Min.Max.Y, 0.5f);
            float thickness = (grab_bb_Min.Max.Y - grab_bb_Min.Min.Y) * 0.5f;
            float xStart = grab_bb_Min.Max.X - 1.0f;
            float xEnd = grab_bb_Max.Min.X;
            if (xEnd > xStart)
                window.DrawList.AddLine(new Vector2(xStart, centerHeight), new Vector2(xEnd, centerHeight), ImGui.GetColorU32(ImGuiCol.SliderGrab, lineAlpha), thickness);

            //finally draw the overall border on top.
            //	ImGui.RenderFrameBorder(frame_bb.Min, frame_bb.Max, g.Style.FrameRounding);

            // Display value using user-provided display format so user can add prefix/suffix/decorations to the value.
            byte* value_buf = stackalloc byte[64];
            byte* value_buf_end = value_buf + ImGui.DataTypeFormatString(value_buf, 64, data_type, p_dataMin, formatMin);
            if (g.LogEnabled)
                ImGuiP.LogSetNextTextDecoration("{", "}");
            ImGuiP.RenderTextClipped(frame_bb.Min, frame_bb.Max, value_buf, value_buf_end, null, new Vector2(0.1f, 0.5f));

            value_buf_end = value_buf + ImGui.DataTypeFormatString(value_buf, 64, data_type, p_dataMax, formatMax);
            if (g.LogEnabled)
                ImGuiP.LogSetNextTextDecoration("{", "}");
            ImGuiP.RenderTextClipped(frame_bb.Min, frame_bb.Max, value_buf, value_buf_end, null, new Vector2(0.9f, 0.5f));

            if (label_size.X > 0.0f)
                ImGuiP.RenderText(new Vector2(frame_bb.Max.X + style.ItemInnerSpacing.X, frame_bb.Min.Y + style.FramePadding.Y), label);

            return minChanged || maxChanged;
        }
    }
}
