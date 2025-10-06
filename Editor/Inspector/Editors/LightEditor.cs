using Editor.DearImGui;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Components;
using Primary.Scenes;
using SharpGen.Runtime;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Editor.Inspector.Editors
{
    [CustomComponentInspector(typeof(Light))]
    internal class LightEditor : ComponentEditor
    {
        private SceneEntity _entity;

        public override void SetupInspectorFields(SceneEntity entity, Type type)
        {
            _entity = entity;
        }

        public override void DrawInspector()
        {
            ref Light light = ref _entity.GetComponent<Light>();

            string lightTypeString = s_lightTypes[(int)light.Type];
            if (ImGuiWidgets.ComboBox("Type:", ref lightTypeString, s_lightTypes))
                light.Type = (LightType)Array.IndexOf(s_lightTypes, lightTypeString);

            ImGui.SeparatorText("Emmision"u8);

            Color diffuseColor = light.Diffuse;
            Color specularColor = light.Specular;
            float brightness = light.Brightness;

            if (light.Type == LightType.SpotLight)
            {
                float innerCone = light.InnerCutOff;
                float outerCone = light.OuterCutOff;
                if (SpotLightConeEditor(ref innerCone, ref outerCone))
                {
                    light.InnerCutOff = innerCone;
                    light.OuterCutOff = outerCone;
                }
            }

            if (ImGuiWidgets.InputColor3("Diffuse:", ref diffuseColor))
                light.Diffuse = diffuseColor;
            if (ImGuiWidgets.InputColor3("Specular:", ref specularColor))
                light.Specular = specularColor;
            if (ImGuiWidgets.InputFloat("Brightness:", ref brightness))
                light.Brightness = brightness;

            ImGui.SeparatorText("Shadows"u8);

            string shadowImportanceString = s_shadowImportance[(int)light.ShadowImportance];
            if (ImGuiWidgets.ComboBox("Shadow importance:", ref shadowImportanceString, s_shadowImportance))
                light.ShadowImportance = (ShadowImportance)Array.IndexOf(s_shadowImportance, shadowImportanceString);
        }

        private static unsafe bool SpotLightConeEditor(ref float inner, ref float outer)
        {
            Vector3 sizing = ImGuiWidgets.Header("Cone:");

            float minValue = 0.0f;
            float maxValue = MathF.PI * 0.5f;

            ImGui.PushID("Cone:");

            ImGui.SetCursorScreenPos(new Vector2(sizing.X, sizing.Y));
            ImGui.SetNextItemWidth(sizing.Z);

            byte def = 1;
            bool ret = ImGuiCustom.RangeSliderScalar("##"u8, ImGuiDataType.Float, Unsafe.AsPointer(ref inner), Unsafe.AsPointer(ref outer), &minValue, &maxValue, true, ""u8.GetPointerUnsafe(), ""u8.GetPointerUnsafe(), ImGuiSliderFlags.AlwaysClamp);

            ImGui.PopID();

            return ret;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            ImGuiContextPtr context = ImGui.GetCurrentContext();

            //Vector3 sizing = ImGuiWidgets.Header("Cone:");


            float innerPerc = Clamp(inner / maxValue, 0.0f, maxValue);
            float outerPerc = Clamp(outer / maxValue, 0.0f, maxValue);

            if (innerPerc > outerPerc)
            {
                float temp = innerPerc;
                innerPerc = outerPerc;
                outerPerc = temp;
            }

            float heightMax = context.FontSize + context.Style.FramePadding.Y * 2.0f;
            float yMiddle = MathF.Floor(sizing.Y + heightMax * 0.5f);
            float percCenter = float.Lerp(innerPerc, outerPerc, 0.5f);

            ImRect bb_inner = new ImRect(new Vector2(sizing.X + innerPerc * sizing.Z - context.Style.FrameRounding - 3.0f, yMiddle - 7.0f), new Vector2(sizing.X + percCenter * sizing.Z + 0.5f, yMiddle + 6.0f));
            ImRect bb_outer = new ImRect(new Vector2(sizing.X + percCenter * sizing.Z - 0.5f, yMiddle - 7.0f), new Vector2(sizing.X + outerPerc * sizing.Z + context.Style.FrameRounding + 3.0f, yMiddle + 6.0f));

            ImRect bb_total = new ImRect(new Vector2(sizing.X, sizing.Y), new Vector2(sizing.X + sizing.Z, sizing.Y + context.FontSize + context.Style.FramePadding.Y * 2.0f));

            uint handleColor = new Color32(context.Style.Colors[(int)ImGuiCol.FrameBg]).ABGR | 0xff000000;
            uint handleColorHovered = new Color32(context.Style.Colors[(int)ImGuiCol.FrameBgHovered]).ABGR | 0xff000000;
            uint handleColorActive = new Color32(context.Style.Colors[(int)ImGuiCol.FrameBgActive]).ABGR | 0xff000000;

            uint innerColor = handleColor;
            uint outerColor = handleColor;

            ImGui.PushID("Cone:");

            byte def1 = 1;
            byte def2 = 2;

            uint id1 = ImGui.GetID(ref def1);
            uint id2 = ImGui.GetID(ref def2);

            ImRect grabBb = new ImRect();

            //ImGuiP.ItemSize(bb_total, context.Style.FramePadding.Y);
            //ImGuiP.ItemAdd(bb_total, id1);
            //
            //bool changed = ImGuiP.SliderBehavior(bb_total, id1, ImGuiDataType.Float, Unsafe.AsPointer(ref inner), &minValue, &maxValue, "%f"u8, ImGuiSliderFlags.None, ref grabBb);
            //Log.Information("{x} - {y} - {z}", inner, changed);
            //bool hovered = false, held = false;
            //if (ImGuiP.ButtonBehavior(bb_inner, id1, ref hovered, ref held))
            //{
            //    ImGuiP.SliderBehavior(bb_inner, id1, ImGuiDataType.Float, )
            //}
            //

            //
            ImGui.SameLine();
            //
            //uint innerColor = held ? handleColorActive : (hovered ? handleColorHovered : handleColor);
            //
            //if (ImGuiP.ButtonBehavior(bb_outer, id2, ref hovered, ref held))
            //{
            //
            //}
            //
            ImGuiP.ItemAdd(bb_outer, id2);
            ImGuiP.ItemSize(bb_outer);
            //
            //uint outerColor = held ? handleColorActive : (hovered ? handleColorHovered : handleColor);

            ImGui.PopID();

            drawList.AddRect(new Vector2(sizing.X, yMiddle - 1.0f), new Vector2(sizing.X + sizing.Z, yMiddle), new Color32(context.Style.Colors[(int)ImGuiCol.FrameBg]).ABGR);
            drawList.AddRectFilled(bb_inner.Min, bb_inner.Max, innerColor, context.Style.FrameRounding, ImDrawFlags.RoundCornersLeft);
            drawList.AddRectFilled(bb_outer.Min, bb_outer.Max, outerColor, context.Style.FrameRounding, ImDrawFlags.RoundCornersRight);

            drawList.AddRectFilled(bb_total.Min, bb_total.Max, 0x80ffffff);

            return false;

            static float Clamp(float v, float min, float max) => MathF.Min(MathF.Max(v, min), max);
        }

        private static string[] s_lightTypes = [
            "Directional",
            "Point",
            "Spot",
            ];

        private static string[] s_shadowImportance = [
          "None",
            "Low",
            "Medium",
            "High",
            ];
    }
}
