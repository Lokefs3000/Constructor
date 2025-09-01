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
        #endregion
    }
}
