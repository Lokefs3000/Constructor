using CommunityToolkit.HighPerformance;
using Hexa.NET.ImGui;
using Primary.GUI.ImGui;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Editor.DearImGui
{
    internal static unsafe class ImGuiWidgets
    {
        #region Core
        public static Vector3 Header(in string headerText)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            return new Vector3(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f), contentAvail.X * 0.6f);
        }

        public static bool ComboBox(byte id, ref string selected, ReadOnlySpan<string> options)
        {
            bool ret = false;

            int hash = selected.GetDjb2HashCode();

            if (ImGui.BeginCombo(&id, selected))
            {
                for (int i = 0; i < options.Length; i++)
                {
                    ref readonly string curr = ref options[i];
                    if (ImGui.Selectable(curr, hash == curr.GetDjb2HashCode()))
                    {
                        selected = curr;
                        ret = true;

                        break;
                    }
                }

                ImGui.EndCombo();
            }

            return ret;
        }
        #endregion

        #region Widgets
        public static bool InputFloat(in string headerText, ref float value, float minimum = float.NegativeInfinity, float maximum = float.PositiveInfinity)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f));
            ImGui.SetNextItemWidth(contentAvail.X * 0.6f);

            byte def = 1;
            bool ret = ImGui.DragFloat(ref def, ref value, 0.1f, minimum, maximum);

            ImGui.PopID();

            return ret;
        }

        public static bool InputVector2(in string headerText, ref Vector2 value, float minimum = float.NegativeInfinity, float maximum = float.PositiveInfinity)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f));
            ImGui.SetNextItemWidth(contentAvail.X * 0.6f);

            byte def = 1;
            bool ret = ImGui.DragFloat2(&def, ref value, 0.1f, minimum, maximum);

            ImGui.PopID();

            return ret;
        }

        public static bool InputVector3(in string headerText, ref Vector3 value, float minimum = float.NegativeInfinity, float maximum = float.PositiveInfinity)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f));
            ImGui.SetNextItemWidth(contentAvail.X * 0.6f);

            byte def = 1;
            bool ret = ImGui.DragFloat3(&def, ref value, 0.1f, minimum, maximum);

            ImGui.PopID();

            return ret;
        }

        public static bool InputInt(in string headerText, ref int value, int minimum = int.MinValue, int maximum = int.MaxValue)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f));
            ImGui.SetNextItemWidth(contentAvail.X * 0.6f);

            byte def = 1;
            bool ret = ImGui.DragInt(ref def, ref value, 0.1f, minimum, maximum);

            ImGui.PopID();

            return ret;
        }

        public static bool SliderInt(in string headerText, ref int value, int minimum = int.MinValue, int maximum = int.MaxValue)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f));
            ImGui.SetNextItemWidth(contentAvail.X * 0.6f);

            byte def = 1;
            bool ret = ImGui.SliderInt(ref def, ref value, minimum, maximum);

            ImGui.PopID();

            return ret;
        }

        public static bool Checkbox(in string headerText, ref bool value)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGuiContextPtr context = ImGui.GetCurrentContext();

            float checkboxSize = context.FontSize + context.Style.FramePadding.Y * 2.0f;

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X - checkboxSize, 0.0f));

            byte def = 1;
            bool ret = ImGui.Checkbox(&def, ref value);

            ImGui.PopID();

            return ret;
        }

        public static bool ComboBox(in string headerText, ref string selected, ReadOnlySpan<string> options)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f));
            ImGui.SetNextItemWidth(contentAvail.X * 0.6f);

            bool ret = false;

            int hash = selected.GetDjb2HashCode();

            byte def = 1;
            if (ImGui.BeginCombo(&def, selected))
            {
                for (int i = 0; i < options.Length; i++)
                {
                    ref readonly string curr = ref options[i];
                    if (ImGui.Selectable(curr, hash == curr.GetDjb2HashCode()))
                    {
                        selected = curr;
                        ret = true;

                        break;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.PopID();

            return ret;
        }

        public static bool CheckedComboBox(in string headerText, ref string selected, ref bool activated, ReadOnlySpan<string> options)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGuiContextPtr context = ImGui.GetCurrentContext();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f));

            bool ret = false;

            int hash = selected.GetDjb2HashCode();

            byte def1 = 1;
            byte def2 = 2;

            ret = ImGui.Checkbox(&def1, ref activated);

            float checkboxSize = context.FontSize + context.Style.FramePadding.Y * 2.0f + context.Style.FramePadding.X;

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X * 0.4f + checkboxSize, 0.0f));
            ImGui.SetNextItemWidth(contentAvail.X * 0.6f - checkboxSize);

            if (!activated)
                ImGui.BeginDisabled();

            if (ImGui.BeginCombo(&def2, selected))
            {
                for (int i = 0; i < options.Length; i++)
                {
                    ref readonly string curr = ref options[i];
                    if (ImGui.Selectable(curr, hash == curr.GetDjb2HashCode()))
                    {
                        selected = curr;
                        ret = true;

                        break;
                    }
                }

                ImGui.EndCombo();
            }

            if (!activated)
                ImGui.EndDisabled();

            ImGui.PopID();

            return ret;
        }
        #endregion
    }
}
