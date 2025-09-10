using Arch.Core.Extensions;
using Editor.DearImGui.Properties;
using Editor.Gui;
using Editor.Interaction;
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

        private DynamicIconSet _iconSet;

        private int _eyeVisibleId;
        private int _eyeHiddenId;
        private int _objectId;

        private HashSet<SceneEntity> _activeEntities;

        internal HierchyView()
        {
            _icons = AssetManager.LoadAsset<TextureAsset>("Content/HierchyIcons.dds", true)!;

            _iconSet = Editor.GlobalSingleton.GuiAtlasManager.CreateIconSet(
                "Content/Icons/EyeVisible.png",
                "Content/Icons/EyeHidden.png",
                "Content/Icons/Object.png");

            _eyeVisibleId = _iconSet.GetIdForIndex(0);
            _eyeHiddenId = _iconSet.GetIdForIndex(1);
            _objectId = _iconSet.GetIdForIndex(2);

            _activeEntities = new HashSet<SceneEntity>();

            SelectionManager selection = Editor.GlobalSingleton.SelectionManager;

            selection.Selected += (x) =>
            {
                if (x is SelectedSceneEntity selected)
                {
                    _activeEntities.Add(selected.Entity);
                }
            };

            selection.Deselected += (x) =>
            {
                if (x is SelectedSceneEntity selected)
                {
                    _activeEntities.Remove(selected.Entity);
                }
            };
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

        private List<(Vector2, int)> _iconPositionStack = new List<(Vector2, int)>();
        private void DrawSceneView(Scene scene)
        {
            _iconPositionStack.Clear();
            if (ImGui.CollapsingHeader(scene.Name, ImGuiTreeNodeFlags.Framed))
            {
                ImGuiContextPtr g = ImGui.GetCurrentContext();
                ImDrawListPtr drawList = ImGui.GetWindowDrawList();

                Vector2 screen = ImGui.GetCursorScreenPos();
                Vector2 avail = ImGui.GetContentRegionAvail();

                float button_sz = g.FontSize + g.Style.FramePadding.Y * 2;

                drawList.AddRectFilled(screen, screen + new Vector2(button_sz, avail.Y), 0x80000000);

                ImGui.Indent(button_sz);
                foreach (SceneEntity entity in scene.Root.Children)
                {
                    RecursiveHierchyView(entity, screen.X + 0.0f);
                }
                ImGui.Unindent();

                ImTextureRef textureRef = ImGuiUtility.GetTextureRef(_iconSet.AtlasTexture.Handle);

                for (int i = 0; i < _iconPositionStack.Count; i++)
                {
                    (Vector2 Position, int Id) = _iconPositionStack[i];
                    if (_iconSet.TryGetAtlasIcon(Id, out DynAtlasIcon icon))
                        drawList.AddImage(textureRef, Position, Position + new Vector2(button_sz), icon.UVs.Minimum, icon.UVs.Maximum, 0x7fffffff);
                }
            }

            void RecursiveHierchyView(SceneEntity entity, float xStart)
            {
                SceneEntityChildren children = entity.Children;
                if (IconTreeNode(entity, xStart))
                {
                    foreach (SceneEntity child in children)
                    {
                        RecursiveHierchyView(child, xStart);
                    }

                    ImGui.TreePop();
                }
            }
        }

        private unsafe bool IconTreeNode(SceneEntity entity, float xStart)
        {
            string label = entity.Name;

            ImGuiContextPtr g = ImGui.GetCurrentContext();
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();

            ImGui.PushID(entity.WrappedEntity.Id);

            uint id = ImGui.GetID(1);
            uint ctx_id = ImGui.GetID(2);
            uint vis_id = ImGui.GetID(3);

            float button_sz = g.FontSize + g.Style.FramePadding.Y * 2;

            Vector2 pos = ImGui.GetCursorScreenPos();
            ImRect full_bb = new ImRect(pos, new Vector2(pos.X + ImGui.GetContentRegionAvail().X, pos.Y + g.FontSize + g.Style.FramePadding.Y * 2));
            ImRect arrow_bb = new ImRect(pos, pos + new Vector2(full_bb.Max.Y - full_bb.Min.Y));
            ImRect context_bb = new ImRect(new Vector2(arrow_bb.Max.X, arrow_bb.Min.Y), full_bb.Max);
            ImRect visible_bb = new ImRect(new Vector2(xStart, full_bb.Min.Y), new Vector2(xStart + button_sz, full_bb.Max.Y));
            bool opened = ImGuiP.TreeNodeGetOpen(id);
            bool hovered, held;

            if (ImGuiP.ButtonBehavior(visible_bb, vis_id, &hovered, &held, ImGuiButtonFlags.MouseButtonLeft))
            {
                entity.Enabled = !entity.Enabled;
            }
            if (hovered || held)
                drawList.AddRectFilled(visible_bb.Min, visible_bb.Max, ImGui.GetColorU32(held ? ImGuiCol.HeaderActive : ImGuiCol.HeaderHovered));

            if (ImGuiP.ButtonBehavior(arrow_bb, id, &hovered, &held, ImGuiButtonFlags.MouseButtonLeft))
                ImGuiP.TreeNodeSetOpen(id, !opened);
            if (hovered || held)
                drawList.AddRectFilled(arrow_bb.Min, arrow_bb.Max, ImGui.GetColorU32(held ? ImGuiCol.HeaderActive : ImGuiCol.HeaderHovered));

            if (ImGuiP.ButtonBehavior(context_bb, ctx_id, &hovered, &held, ImGuiButtonFlags.MouseButtonLeft))
            {
                SelectionManager selection = Editor.GlobalSingleton.SelectionManager;
                if (g.IO.KeyCtrl > 0)
                    selection.AddSelectedAndSetContext(new SelectedSceneEntity { Entity = entity });
                else
                    selection.SetOnlySelectedContext(new SelectedSceneEntity { Entity = entity });
            }
            if (hovered || held)
                drawList.AddRectFilled(context_bb.Min, context_bb.Max, ImGui.GetColorU32(held ? ImGuiCol.HeaderActive : ImGuiCol.HeaderHovered));

            // Icon, text
            if (_activeEntities.Contains(entity))
            {
                SelectionManager selection = Editor.GlobalSingleton.SelectionManager;
                drawList.AddRectFilled(full_bb.Min, full_bb.Max, new Color(g.Style.Colors[(selection.CurrentContext is SelectedSceneEntity selected && selected.Entity == entity) ? (int)ImGuiCol.FrameBgActive : (int)ImGuiCol.FrameBg]).ToColor32().ARGB);
            }
            if (!entity.Children.IsEmpty)
                ImGuiP.RenderArrow(drawList, pos + new Vector2(3.0f), 0xffffffff, opened ? ImGuiDir.Down : ImGuiDir.Right);
            _iconPositionStack.Add((pos + new Vector2(button_sz, 0.0f), _objectId));
            _iconPositionStack.Add((visible_bb.Min, entity.Enabled ? _eyeVisibleId : _eyeHiddenId));
            drawList.AddText(new Vector2(pos.X + button_sz * 2.0f + g.Style.ItemInnerSpacing.X, pos.Y + g.Style.FramePadding.Y), 0xffffffff, label);

            ImGuiP.ItemAdd(arrow_bb, id);
            ImGuiP.ItemAdd(context_bb, ctx_id);
            ImGuiP.ItemAdd(visible_bb, vis_id);

            ImGuiP.ItemSize(full_bb, g.Style.FramePadding.Y);

            ImGui.PopID();

            if (opened)
                ImGui.TreePush(label);
            return opened;
        }
    }
}
