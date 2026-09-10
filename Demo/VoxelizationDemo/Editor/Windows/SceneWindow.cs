using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Input;
using Primary.IO;
using Primary.Scenes;
using Primary.Threading;
using Primary.Windowing;
using PrimaryEditor.Assets;
using PrimaryEditor.Inspector.Contexts.Entity;
using PrimaryEditor.Project;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Utility;

namespace VoxelizationDemo.Editor.Windows
{
    internal sealed class SceneWindow : IEditorWindow
    {
        private Scene? _currentHoveredScene;
        private SceneEntity? _currentHoveredEntity;

        public bool UpdateAndRender()
        {
            bool isContextMenuOpen = false;

            if (ImGui.Begin("Scenes"u8))
            {
                VoxelRuntime runtime = VoxelRuntime.Instance;
                foreach (Scene scene in runtime.SceneManager.Scenes)
                {
                    float cursorY = ImGui.GetCursorScreenPos().Y;
                    if (cursorY <= InputSystem.Pointer.MousePosition.Y)
                    {
                        _currentHoveredScene = scene;
                    }

                    if (DrawSceneNodeHeader(runtime, scene))
                    {
                        if (scene.Root.Children.Count == 0)
                        {
                            ImGui.NewLine();
                        }
                        else
                        {
                            foreach (SceneEntity child in scene.Root.Children)
                            {
                                IterateNodeRecursive(runtime, child);
                            }
                        }

                        ImGui.Unindent();
                        ImGui.PopID();
                    }
                }

                if (ImGui.BeginPopupContextWindow())
                {
                    isContextMenuOpen = true;

                    if (ImGui.BeginMenu("Add"u8, _currentHoveredScene != null))
                    {
                        SceneEntity parent = _currentHoveredEntity ?? SceneEntity.Null;

                        if (ImGui.MenuItem("Empty entity"u8))
                        {
                            _currentHoveredScene?.CreateEntity(parent);
                        }

                        ImGui.EndMenu();
                    }

                    if (ImGui.BeginMenu("Load scene"u8))
                    {
                        if (ImGui.MenuItem("Single"u8))
                            TryOpenNewScene(runtime, LoadSceneMode.Single);
                        if (ImGui.MenuItem("Additive"u8))
                            TryOpenNewScene(runtime, LoadSceneMode.Additive);

                        ImGui.EndMenu();
                    }

                    ImGui.EndPopup();
                }
            }
            ImGui.End();

            if (!isContextMenuOpen)
            {
                _currentHoveredScene = null;
                _currentHoveredEntity = null;
            }

            return true;
        }

        private void IterateNodeRecursive(VoxelRuntime runtime, SceneEntity entity)
        {
            if (DrawEntityNode(runtime, entity))
            {
                foreach (SceneEntity child in entity.Children)
                {
                    DrawEntityNode(runtime, child);
                }

                ImGui.Unindent();
                ImGui.PopID();
            }
        }

        private void TryOpenNewScene(VoxelRuntime runtime, LoadSceneMode loadMode)
        {
            Window? window = runtime.WindowManager.PrimaryWindow;

            Task.Factory.StartNew(() =>
            {
                OpenFileDialogResult dialogResult = FileDialog.OpenFile(new OpenFileDialogParams
                {
                    DefaultDirectory = ProjectData.Instance!.Paths.ContentFolder,
                    Filters = [new FileFilter("Scene files", "*.scene")]
                }, window);

                if (dialogResult.Result == FileDialogResult.Ok)
                {
                    string fileName = dialogResult.Files[0];
                    ThreadHelper.ExecuteOnMainThread(() => LoadScenesWithMode(fileName));
                }
            });

            void LoadScenesWithMode(string fileName)
            {
                if (!FilesystemManager.TryGetLocalPath(fileName, out string? localPath))
                {
                    return;
                }

                runtime.SceneManager.LoadScene(localPath, loadMode);
            }
        }

        private static bool DrawSceneNodeHeader(VoxelRuntime runtime, Scene scene)
        {
            ImGuiContextPtr context = runtime.EditorManager.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            ImGui.PushID(scene.Id | (1 << 30));

            uint arrowId = ImGuiP.GetID(window, 1);
            uint headerId = ImGuiP.GetID(window, 2);

            ImGui.PopID();

            Vector2 cursorPos = window.DC.CursorPos;

            ImRect bb = new ImRect(cursorPos, cursorPos + new Vector2(ImGui.GetContentRegionAvail().X, context.FontSize + context.Style.FramePadding.Y * 2.0f));

            ImRect arrowBb = new ImRect(cursorPos, bb.Min + new Vector2(bb.Max.Y - bb.Min.Y));
            ImRect headerBb = new ImRect(bb.Min.WithElement(0, arrowBb.Max.X), bb.Max);

            unsafe
            {
                if (window.StateStorage.GetIntRef(headerId) == null)
                    ImGuiP.TreeNodeSetOpen(headerId, true);
            }

            bool isOpened = ImGuiP.TreeNodeGetOpen(headerId);

            bool isArrowHovered = false;
            bool isArrowHeld = false;

            bool isHeaderHovered = false;
            bool isHeaderHeld = false;

            if (ImGuiP.ButtonBehavior(arrowBb, arrowId, ref isArrowHovered, ref isArrowHeld))
                ImGuiP.TreeNodeSetOpen(headerId, !isOpened);

            if (ImGuiP.ButtonBehavior(headerBb, headerId, ref isHeaderHovered, ref isHeaderHeld))
                ;

            ImGuiCol backgroundColor = (isArrowHeld || isHeaderHeld) ? ImGuiCol.FrameBgActive : ((isArrowHovered || isHeaderHovered) ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg);
            window.DrawList.AddRectFilled(arrowBb.Min, arrowBb.Max, ImGui.GetColorU32(backgroundColor));

            backgroundColor = isHeaderHeld ? ImGuiCol.FrameBgActive : (isHeaderHovered ? ImGuiCol.FrameBgHovered : ImGuiCol.FrameBg);
            window.DrawList.AddRectFilled(headerBb.Min, headerBb.Max, ImGui.GetColorU32(backgroundColor));

            ImGuiP.RenderArrow(window.DrawList, arrowBb.Min + context.Style.FramePadding, 0xffffffff, isOpened ? ImGuiDir.Down : ImGuiDir.Up);
            ImGuiP.RenderText(headerBb.Min + new Vector2(context.Style.ItemInnerSpacing.X, context.Style.FramePadding.Y), scene.Name);

            if (runtime.EditorManager.EditorState.PrimaryScene == scene)
            {
                float height = bb.Max.Y - bb.Min.Y;
                float shapeSize = height * 0.25f;
                Vector2 center = new Vector2(bb.Max.X - height * 0.5f, bb.Min.Y + height * 0.5f);

                window.DrawList.AddQuadFilled(
                    center.WithElement(0, center.X - shapeSize),
                    center.WithElement(1, center.Y - shapeSize),
                    center.WithElement(0, center.X + shapeSize),
                    center.WithElement(1, center.Y + shapeSize),
                    0xff00ffff);

                window.DrawList.AddRect(bb.Min, bb.Max, ImGui.GetColorU32(ImGuiCol.HeaderActive));
            }

            ImGuiP.ItemSize(bb, context.Style.FramePadding.Y);
            ImGuiP.ItemAdd(arrowBb, arrowId);
            ImGuiP.ItemAdd(headerBb, headerId);

            if (isOpened)
            {
                ImGui.Indent();
                ImGui.PushID(scene.Id);
            }

            return isOpened;
        }

        private bool DrawEntityNode(VoxelRuntime runtime, SceneEntity entity)
        {
            ImGuiContextPtr context = runtime.EditorManager.ContextManager.ImGuiCtx;
            ImGuiWindowPtr window = context.CurrentWindow;

            string entityName = entity.Name;

            ImGui.PushID(entity.WrappedEntity.Id | (1 << 30));

            uint arrowId = ImGuiP.GetID(window, 1);
            uint headerId = ImGuiP.GetID(window, 2);

            ImGui.PopID();

            Vector2 cursorPos = window.DC.CursorPos;

            ImRect bb = new ImRect(cursorPos, cursorPos + new Vector2(ImGui.GetContentRegionAvail().X, context.FontSize + context.Style.FramePadding.Y * 2.0f));

            ImRect arrowBb = new ImRect(cursorPos, bb.Min + new Vector2(bb.Max.Y - bb.Min.Y));
            ImRect headerBb = new ImRect(bb.Min.WithElement(0, arrowBb.Max.X), bb.Max);

            bool isOpened = ImGuiP.TreeNodeGetOpen(headerId);

            bool isArrowHovered = false;
            bool isArrowHeld = false;

            bool isHeaderHovered = false;
            bool isHeaderHeld = false;

            if (ImGuiP.ButtonBehavior(arrowBb, arrowId, ref isArrowHovered, ref isArrowHeld))
                ImGuiP.TreeNodeSetOpen(headerId, !isOpened);

            if (ImGuiP.ButtonBehavior(headerBb, headerId, ref isHeaderHovered, ref isHeaderHeld))
                Select.Add(entity, true);

            if (Select.ActiveEntity == entity)
                isHeaderHovered = true;

            ImGuiCol backgroundColor = (isArrowHeld || isHeaderHeld) ? ImGuiCol.HeaderActive : ((isArrowHovered || isHeaderHovered) ? ImGuiCol.HeaderHovered : ImGuiCol.Header);
            if (backgroundColor != ImGuiCol.Header)
                window.DrawList.AddRectFilled(arrowBb.Min, arrowBb.Max, ImGui.GetColorU32(backgroundColor));

            backgroundColor = isHeaderHeld ? ImGuiCol.HeaderActive : (isHeaderHovered ? ImGuiCol.HeaderHovered : ImGuiCol.Header);
            if (backgroundColor != ImGuiCol.Header)
                window.DrawList.AddRectFilled(headerBb.Min, headerBb.Max, ImGui.GetColorU32(backgroundColor));

            ImGuiP.RenderArrow(window.DrawList, arrowBb.Min + context.Style.FramePadding, 0xffffffff, isOpened ? ImGuiDir.Down : ImGuiDir.Up);
            ImGuiP.RenderText(headerBb.Min + new Vector2(context.Style.ItemInnerSpacing.X, context.Style.FramePadding.Y), entityName);

            ImGuiP.ItemSize(bb, context.Style.FramePadding.Y);
            ImGuiP.ItemAdd(arrowBb, arrowId);
            ImGuiP.ItemAdd(headerBb, headerId);

            if (isOpened)
            {
                ImGui.Indent();
                ImGui.PushID(entity.WrappedEntity.Id);
            }

            if (isArrowHovered || isHeaderHovered)
            {
                _currentHoveredEntity = entity;
            }

            return isOpened;
        }
    }
}
