using CommunityToolkit.HighPerformance;
using Editor.Assets;
using Editor.DearImGui.Popups;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Rendering.Data;
using System.Numerics;
using System.Runtime.CompilerServices;

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

        public static Vector3 Header(in ReadOnlySpan<byte> headerText)
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

        public static bool InputVector4(in string headerText, ref Vector4 value, float minimum = float.NegativeInfinity, float maximum = float.PositiveInfinity)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f));
            ImGui.SetNextItemWidth(contentAvail.X * 0.6f);

            byte def = 1;
            bool ret = ImGui.DragFloat4(&def, ref value, 0.1f, minimum, maximum);

            ImGui.PopID();

            return ret;
        }

        public static bool InputColor3(in string headerText, ref Color value)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f));
            ImGui.SetNextItemWidth(contentAvail.X * 0.6f);

            byte def = 1;
            bool ret = ImGui.ColorEdit3(&def, ref Unsafe.As<Color, float>(ref value), ImGuiColorEditFlags.Float);

            ImGui.PopID();

            return ret;
        }

        public static bool InputColor4(in string headerText, ref Color value)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            ImGui.SetCursorScreenPos(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f));
            ImGui.SetNextItemWidth(contentAvail.X * 0.6f);

            byte def = 1;
            bool ret = ImGui.ColorEdit4(&def, ref Unsafe.As<Color, float>(ref value), ImGuiColorEditFlags.Float);

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

        public static bool SelectorMesh(in string headerText, RenderMesh? value, Action<RenderMesh> update)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            byte def1 = 1;
            byte def2 = 2;

            uint id1 = ImGui.GetID(&def1);
            uint id2 = ImGui.GetID(&def2);

            bool hovered, held;

            string renderMeshName = value?.Id ?? "null";

            float buttonWidth = context.Style.FramePadding.Y * 2.0f + context.FontSize;

            ImRect bb = new ImRect(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f), screenCursor + new Vector2(contentAvail.X, buttonWidth));
            ImRect disp_bb = new ImRect(bb.Min, new Vector2(bb.Max.X - buttonWidth, bb.Max.Y));
            ImRect button_bb = new ImRect(new Vector2(bb.Max.X - buttonWidth, bb.Min.Y), bb.Max);

            Vector2 textSize = ImGui.CalcTextSize(renderMeshName);

            drawList.AddRectFilled(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.FrameBg]).ABGR, context.Style.FrameRounding);
            drawList.AddRect(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR, context.Style.FrameRounding);
            drawList.AddText(Vector2.Lerp(disp_bb.Min, disp_bb.Max, 0.5f) - textSize * 0.5f, 0xffffffff, renderMeshName);

            ImGuiP.ItemAdd(bb, id1);
            ImGuiP.ItemSize(bb);

            ImGui.SameLine();

            if (value != null)
            {
                if (ImGui.BeginItemTooltip())
                {
                    ImGui.TextUnformatted($"Model: {value.Model}");

                    ImGui.Separator();

                    ImGui.TextUnformatted($"Vertex offset: {value.VertexOffset}");
                    ImGui.TextUnformatted($"Index offset: {value.IndexOffset}");
                    ImGui.TextUnformatted($"Index count: {value.IndexCount}");

                    ImGui.EndTooltip();
                }
            }

            if (ImGuiP.ButtonBehavior(button_bb, id2, &hovered, &held))
            {
                Editor.GlobalSingleton.PopupManager.Open(new ModelAssetPicker(update));
            }

            drawList.AddRectFilled(button_bb.Min, button_bb.Max, new Color32(context.Style.Colors[(int)(held ? ImGuiCol.ButtonActive : (hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button))]).ABGR, context.Style.FrameRounding);
            drawList.AddRect(button_bb.Min, button_bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR, context.Style.FrameRounding);

            drawList.AddCircle(Vector2.Lerp(button_bb.Min, button_bb.Max, 0.5f), 4.0f, 0xffffffff);

            ImGuiP.ItemAdd(bb, id2);
            ImGuiP.ItemSize(bb);

            ImGui.PopID();

            return false;
        }

        public static bool SelectorRawRenderMesh(in string headerText, RawRenderMesh? value, Action<RawRenderMesh> update)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            byte def1 = 1;
            byte def2 = 2;

            uint id1 = ImGui.GetID(&def1);
            uint id2 = ImGui.GetID(&def2);

            bool hovered, held;

            string renderMeshName = Unsafe.As<RenderMesh>(value)?.Id ?? "null";

            float buttonWidth = context.Style.FramePadding.Y * 2.0f + context.FontSize;

            ImRect bb = new ImRect(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f), screenCursor + new Vector2(contentAvail.X, buttonWidth));
            ImRect disp_bb = new ImRect(bb.Min, new Vector2(bb.Max.X - buttonWidth, bb.Max.Y));
            ImRect button_bb = new ImRect(new Vector2(bb.Max.X - buttonWidth, bb.Min.Y), bb.Max);

            Vector2 textSize = ImGui.CalcTextSize(renderMeshName);

            drawList.AddRectFilled(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.FrameBg]).ABGR, context.Style.FrameRounding);
            drawList.AddRect(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR, context.Style.FrameRounding);
            drawList.AddText(Vector2.Lerp(disp_bb.Min, disp_bb.Max, 0.5f) - textSize * 0.5f, 0xffffffff, renderMeshName);

            ImGuiP.ItemAdd(bb, id1);
            ImGuiP.ItemSize(bb);

            ImGui.SameLine();

            if (value != null)
            {
                if (ImGui.BeginItemTooltip())
                {
                    ImGui.TextUnformatted($"Model: {Unsafe.As<RenderMesh>(value).Model}");

                    ImGui.Separator();

                    ImGui.TextUnformatted($"Vertex offset: {value.VertexOffset}");
                    ImGui.TextUnformatted($"Index offset: {value.IndexOffset}");
                    ImGui.TextUnformatted($"Index count: {value.IndexCount}");

                    ImGui.EndTooltip();
                }
            }

            if (ImGuiP.ButtonBehavior(button_bb, id2, &hovered, &held))
            {
                Editor.GlobalSingleton.PopupManager.Open(new ModelAssetPicker(update));
            }

            drawList.AddRectFilled(button_bb.Min, button_bb.Max, new Color32(context.Style.Colors[(int)(held ? ImGuiCol.ButtonActive : (hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button))]).ABGR, context.Style.FrameRounding);
            drawList.AddRect(button_bb.Min, button_bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR, context.Style.FrameRounding);

            drawList.AddCircle(Vector2.Lerp(button_bb.Min, button_bb.Max, 0.5f), 4.0f, 0xffffffff);

            ImGuiP.ItemAdd(bb, id2);
            ImGuiP.ItemSize(bb);

            ImGui.PopID();

            return false;
        }

        public static bool SelectorMaterial(in string headerText, MaterialAsset? value, Action<MaterialAsset> update)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            byte def1 = 1;
            byte def2 = 2;

            uint id1 = ImGui.GetID(&def1);
            uint id2 = ImGui.GetID(&def2);

            bool hovered, held;

            string materialName = value?.Name ?? "null";

            float buttonWidth = context.Style.FramePadding.Y * 2.0f + context.FontSize;

            ImRect bb = new ImRect(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f), screenCursor + new Vector2(contentAvail.X, buttonWidth));
            ImRect disp_bb = new ImRect(bb.Min, new Vector2(bb.Max.X - buttonWidth, bb.Max.Y));
            ImRect button_bb = new ImRect(new Vector2(bb.Max.X - buttonWidth, bb.Min.Y), bb.Max);

            Vector2 textSize = ImGui.CalcTextSize(materialName);

            drawList.AddRectFilled(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.FrameBg]).ABGR, context.Style.FrameRounding);
            drawList.AddRect(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR, context.Style.FrameRounding);
            drawList.AddText(Vector2.Lerp(disp_bb.Min, disp_bb.Max, 0.5f) - textSize * 0.5f, 0xffffffff, materialName);

            ImGuiP.ItemAdd(bb, id1);
            ImGuiP.ItemSize(bb);

            ImGui.SameLine();

            if (ImGuiP.ButtonBehavior(button_bb, id2, &hovered, &held))
            {
                Editor.GlobalSingleton.PopupManager.Open(new MaterialAssetPicker(update));
            }

            drawList.AddRectFilled(button_bb.Min, button_bb.Max, new Color32(context.Style.Colors[(int)(held ? ImGuiCol.ButtonActive : (hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button))]).ABGR, context.Style.FrameRounding);
            drawList.AddRect(button_bb.Min, button_bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR, context.Style.FrameRounding);

            drawList.AddCircle(Vector2.Lerp(button_bb.Min, button_bb.Max, 0.5f), 4.0f, 0xffffffff);

            ImGuiP.ItemAdd(bb, id2);
            ImGuiP.ItemSize(bb);

            ImGui.PopID();

            return false;
        }

        public static bool SelectorAsset<T>(in string headerText, T? value, Action<T> update) where T : class, IAssetDefinition
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            byte def1 = 1;
            byte def2 = 2;

            uint id1 = ImGui.GetID(&def1);
            uint id2 = ImGui.GetID(&def2);

            bool hovered, held;

            string assetName = value?.Name ?? "null";

            float buttonWidth = context.Style.FramePadding.Y * 2.0f + context.FontSize;

            ImRect bb = new ImRect(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f), screenCursor + new Vector2(contentAvail.X, buttonWidth));
            ImRect disp_bb = new ImRect(bb.Min, new Vector2(bb.Max.X - buttonWidth, bb.Max.Y));
            ImRect button_bb = new ImRect(new Vector2(bb.Max.X - buttonWidth, bb.Min.Y), bb.Max);

            Vector2 textSize = ImGui.CalcTextSize(assetName);

            drawList.AddRectFilled(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.FrameBg]).ABGR, context.Style.FrameRounding);
            drawList.AddRect(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR, context.Style.FrameRounding);
            drawList.AddText(Vector2.Lerp(disp_bb.Min, disp_bb.Max, 0.5f) - textSize * 0.5f, 0xffffffff, assetName);

            ImGuiP.ItemAdd(bb, id1);
            ImGuiP.ItemSize(bb);

            if (ImGui.BeginItemTooltip())
            {
                if (value != null)
                {
                    ImGui.TextUnformatted(value.Name);
                    ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 0.5f), "Id: " + value.Id);
                    ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 0.5f), "Status: " + value.Status);
                }
                
                ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 0.5f), typeof(T).Name);

                ImGui.EndTooltip();
            }

            ImGui.SameLine();

            if (ImGuiP.ButtonBehavior(button_bb, id2, &hovered, &held))
            {
                Editor.GlobalSingleton.PopupManager.Open(new AssetPicker(typeof(T), value?.Id ?? AssetId.Invalid, (x) => update((x as T)!), null));
            }

            drawList.AddRectFilled(button_bb.Min, button_bb.Max, new Color32(context.Style.Colors[(int)(held ? ImGuiCol.ButtonActive : (hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button))]).ABGR, context.Style.FrameRounding);
            drawList.AddRect(button_bb.Min, button_bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR, context.Style.FrameRounding);

            drawList.AddCircle(Vector2.Lerp(button_bb.Min, button_bb.Max, 0.5f), 4.0f, 0xffffffff);

            ImGuiP.ItemAdd(bb, id2);
            ImGuiP.ItemSize(bb);

            ImGui.PopID();

            return false;
        }

        public static bool SelectorAsset(in string headerText, Type type, IAssetDefinition? value, Action<IAssetDefinition> update)
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            byte def1 = 1;
            byte def2 = 2;

            uint id1 = ImGui.GetID(&def1);
            uint id2 = ImGui.GetID(&def2);

            bool hovered, held;

            string assetName = value?.Name ?? "null";

            float buttonWidth = context.Style.FramePadding.Y * 2.0f + context.FontSize;

            ImRect bb = new ImRect(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f), screenCursor + new Vector2(contentAvail.X, buttonWidth));
            ImRect disp_bb = new ImRect(bb.Min, new Vector2(bb.Max.X - buttonWidth, bb.Max.Y));
            ImRect button_bb = new ImRect(new Vector2(bb.Max.X - buttonWidth, bb.Min.Y), bb.Max);

            Vector2 textSize = ImGui.CalcTextSize(assetName);

            drawList.AddRectFilled(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.FrameBg]).ABGR, context.Style.FrameRounding);
            drawList.AddRect(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR, context.Style.FrameRounding);
            drawList.AddText(Vector2.Lerp(disp_bb.Min, disp_bb.Max, 0.5f) - textSize * 0.5f, 0xffffffff, assetName);

            ImGuiP.ItemAdd(bb, id1);
            ImGuiP.ItemSize(bb);

            if (ImGui.BeginItemTooltip())
            {
                if (value != null)
                {
                    ImGui.TextUnformatted(value.Name);
                    ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 0.5f), "Id: " + value.Id);
                    ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 0.5f), "Status: " + value.Status);
                }

                ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 0.5f), type.Name);

                ImGui.EndTooltip();
            }

            ImGui.SameLine();

            if (ImGuiP.ButtonBehavior(button_bb, id2, &hovered, &held))
            {
                Editor.GlobalSingleton.PopupManager.Open(new AssetPicker(type, value?.Id ?? AssetId.Invalid, update, null));
            }

            drawList.AddRectFilled(button_bb.Min, button_bb.Max, new Color32(context.Style.Colors[(int)(held ? ImGuiCol.ButtonActive : (hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button))]).ABGR, context.Style.FrameRounding);
            drawList.AddRect(button_bb.Min, button_bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR, context.Style.FrameRounding);

            drawList.AddCircle(Vector2.Lerp(button_bb.Min, button_bb.Max, 0.5f), 4.0f, 0xffffffff);

            ImGuiP.ItemAdd(bb, id2);
            ImGuiP.ItemSize(bb);

            ImGui.PopID();

            return false;
        }

        public static bool SelectorAssetId<T>(in string headerText, AssetId id, Action<AssetId> update) where T : class, IAssetDefinition
        {
            Vector2 contentAvail = ImGui.GetContentRegionAvail();
            Vector2 screenCursor = ImGui.GetCursorScreenPos();

            ImGuiContextPtr context = ImGui.GetCurrentContext();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            AssetPipeline pipeline = Editor.GlobalSingleton.AssetPipeline;

            ImGui.TextUnformatted(headerText);
            ImGui.SameLine();

            ImGui.PushID(headerText);

            byte def1 = 1;
            byte def2 = 2;

            uint id1 = ImGui.GetID(&def1);
            uint id2 = ImGui.GetID(&def2);

            bool hovered, held;

            string? path = pipeline.Identifier.RetrievePathForId(id);
            string assetName = Path.GetFileNameWithoutExtension(path) ?? "null";

            float buttonWidth = context.Style.FramePadding.Y * 2.0f + context.FontSize;

            ImRect bb = new ImRect(screenCursor + new Vector2(contentAvail.X * 0.4f, 0.0f), screenCursor + new Vector2(contentAvail.X, buttonWidth));
            ImRect disp_bb = new ImRect(bb.Min, new Vector2(bb.Max.X - buttonWidth, bb.Max.Y));
            ImRect button_bb = new ImRect(new Vector2(bb.Max.X - buttonWidth, bb.Min.Y), bb.Max);

            Vector2 textSize = ImGui.CalcTextSize(assetName);

            drawList.AddRectFilled(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.FrameBg]).ABGR, context.Style.FrameRounding);
            drawList.AddRect(bb.Min, bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR, context.Style.FrameRounding);
            drawList.AddText(Vector2.Lerp(disp_bb.Min, disp_bb.Max, 0.5f) - textSize * 0.5f, 0xffffffff, assetName);

            ImGuiP.ItemAdd(bb, id1);
            ImGuiP.ItemSize(bb);

            if (ImGui.BeginItemTooltip())
            {
                ImGui.TextUnformatted("Id: " + id);
                ImGui.TextColored(new Vector4(1.0f, 1.0f, 1.0f, 0.5f), typeof(T).Name);
            
                ImGui.EndTooltip();
            }

            ImGui.SameLine();

            if (ImGuiP.ButtonBehavior(button_bb, id2, &hovered, &held))
            {
                Editor.GlobalSingleton.PopupManager.Open(new AssetPicker(typeof(T), id, null, update));
            }

            drawList.AddRectFilled(button_bb.Min, button_bb.Max, new Color32(context.Style.Colors[(int)(held ? ImGuiCol.ButtonActive : (hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button))]).ABGR, context.Style.FrameRounding);
            drawList.AddRect(button_bb.Min, button_bb.Max, new Color32(context.Style.Colors[(int)ImGuiCol.Border]).ABGR, context.Style.FrameRounding);

            drawList.AddCircle(Vector2.Lerp(button_bb.Min, button_bb.Max, 0.5f), 4.0f, 0xffffffff);

            ImGuiP.ItemAdd(bb, id2);
            ImGuiP.ItemSize(bb);

            ImGui.PopID();

            return false;
        }
        #endregion
    }
}
