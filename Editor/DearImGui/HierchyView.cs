using Arch.Core.Extensions;
using Editor.DearImGui.Properties;
using Hexa.NET.ImGui;
using Primary.Assets;
using Primary.Common;
using Primary.GUI.ImGui;
using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui
{
    internal sealed class HierchyView
    {
        private TextureAsset _icons;

        internal HierchyView()
        {
            _icons = AssetManager.LoadAsset<TextureAsset>("Content/HierchyIcons.dds", true)!;
        }

        internal void Render()
        {
            if (ImGui.Begin("Hierchy"))
            {
                if (ImGui.BeginChild("##HIERCHY_VIEW"))
                {
                    SceneManager sceneManager = Editor.GlobalSingleton.SceneManager;
                    foreach (Scene scene in sceneManager.Scenes)
                    {
                        DrawSceneView(scene);
                    }

                    ImGui.EndChild();
                }
            }
            ImGui.End();
        }

        private List<Vector2> _iconPositionStack = new List<Vector2>();
        private void DrawSceneView(Scene scene)
        {
            _iconPositionStack.Clear();
            if (ImGui.CollapsingHeader(scene.Name, ImGuiTreeNodeFlags.Framed))
            {
                ImGui.Indent(8.0f);
                foreach (SceneEntity entity in scene.Root.Children)
                {
                    RecursiveHierchyView(entity);
                }
                ImGui.Unindent();
                
                unsafe
                {
                    ImGuiContextPtr g = ImGui.GetCurrentContext();
                    ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                    float button_sz = g.FontSize + g.Style.FramePadding.Y * 2;
                    for (int i = 0; i < _iconPositionStack.Count; i++)
                    {
                        Vector2 pos = _iconPositionStack[i];
                        drawList.AddImage(new ImTextureRef(null, new ImTextureID(_icons.Handle)), pos, pos + new Vector2(button_sz));
                    }
                }
            }

            void RecursiveHierchyView(SceneEntity entity)
            {
                SceneEntityChildren children = entity.Children;
                if (IconTreeNode(entity))
                {
                    foreach (SceneEntity child in children)
                    {
                        RecursiveHierchyView(child);
                    }

                    ImGui.TreePop();
                }
            }
        }

        private unsafe bool IconTreeNode(SceneEntity entity)
        {
            string label = entity.Name;

            ImGuiContextPtr g = ImGui.GetCurrentContext();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            ImGui.PushID(entity.WrappedEntity.Id);

            uint id = ImGui.GetID("a");
            uint ctx_id = ImGui.GetID("b");

            Vector2 pos = ImGui.GetCursorScreenPos();
            ImRect full_bb = new ImRect(pos, new Vector2(pos.X + ImGui.GetContentRegionAvail().X, pos.Y + g.FontSize + g.Style.FramePadding.Y * 2));
            ImRect arrow_bb = new ImRect(pos, pos + new Vector2(full_bb.Max.Y - full_bb.Min.Y));
            ImRect context_bb = new ImRect(new Vector2(arrow_bb.Max.X, arrow_bb.Min.Y), full_bb.Max);
            bool opened = ImGuiP.TreeNodeGetOpen(id);
            bool hovered, held;

            if (ImGuiP.ButtonBehavior(arrow_bb, id, &hovered, &held, ImGuiButtonFlags.MouseButtonLeft))
                ImGuiP.TreeNodeSetOpen(id, !opened);
            if (hovered || held)
                drawList.AddRectFilled(arrow_bb.Min, arrow_bb.Max, ImGui.GetColorU32(held ? ImGuiCol.HeaderActive : ImGuiCol.HeaderHovered));

            if (ImGuiP.ButtonBehavior(context_bb, ctx_id, &hovered, &held, ImGuiButtonFlags.MouseButtonLeft))
                Editor.GlobalSingleton.PropertiesView.SetInspected(new EntityProperties.TargetData(entity));
            if (hovered || held)
                drawList.AddRectFilled(context_bb.Min, context_bb.Max, ImGui.GetColorU32(held ? ImGuiCol.HeaderActive : ImGuiCol.HeaderHovered));

            // Icon, text
            float button_sz = g.FontSize + g.Style.FramePadding.Y * 2;
            if (!entity.Children.IsEmpty)
                ImGuiP.RenderArrow(drawList, pos + new Vector2(3.0f), 0xffffffff, opened ? ImGuiDir.Down : ImGuiDir.Right);
            _iconPositionStack.Add(pos + new Vector2(button_sz, 0.0f));
            drawList.AddText(new Vector2(pos.X + button_sz * 2.0f + g.Style.ItemInnerSpacing.X, pos.Y + g.Style.FramePadding.Y), 0xffffffff, label);

            ImGuiP.ItemAdd(arrow_bb, id);
            ImGuiP.ItemAdd(context_bb, ctx_id);

            ImGuiP.ItemSize(full_bb, g.Style.FramePadding.Y);

            ImGui.PopID();

            if (opened)
                ImGui.TreePush(label);
            return opened;
        }
    }
}
