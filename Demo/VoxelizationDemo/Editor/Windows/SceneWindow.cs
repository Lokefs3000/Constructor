using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Collections;
using Primary.Common;
using Primary.Input;
using Primary.IO;
using Primary.Scenes;
using Primary.Streams;
using Primary.Threading;
using Primary.Windowing;
using PrimaryEditor.Assets;
using PrimaryEditor.Inspector.Contexts.Entity;
using PrimaryEditor.Project;
using PrimaryEditor.Selection;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Serialization.Special;
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

                    if (_currentHoveredScene != null)
                    {
                        ImGui.Separator();
                        if (_currentHoveredEntity.HasValue || Select.ActiveEntity != SceneEntity.Null)
                        {
                            if (ImGui.MenuItem("Cut"u8))
                            {

                            }

                            if (ImGui.MenuItem("Copy"u8))
                            {
                                AddSelectionToClipboard(_currentHoveredEntity);
                            }

                            if (ImGui.MenuItem("Duplicate"u8))
                            {
                                string data = GetSelectionAsString(_currentHoveredEntity);
                                PasteFromString(data, _currentHoveredScene.Root);
                            }
                        }

                        if (ImGui.MenuItem("Paste"u8))
                        {
                            PasteFromClipboard(_currentHoveredEntity.GetValueOrDefault(_currentHoveredScene.Root));
                        }
                    }

                    ImGui.Separator();

                    if (ImGui.BeginMenu("Save scene"u8))
                    {
                        if (ImGui.MenuItem("All"))
                            TrySaveScene(runtime, null, true);

                        if (_currentHoveredScene != null)
                        {
                            ImGui.Separator();

                            if (ImGui.MenuItem("Overwrite"u8))
                                TrySaveScene(runtime, _currentHoveredScene, true);
                            if (ImGui.MenuItem("Save new"u8))
                                TrySaveScene(runtime, _currentHoveredScene, false);
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

                    if (ImGui.BeginMenu("New scene"u8))
                    {
                        if (ImGui.MenuItem("Single"u8))
                            ThreadHelper.ExecuteOnMainThread(() => VoxelRuntime.Instance.SceneManager.CreateScene("Unititled", LoadSceneMode.Single));
                        if (ImGui.MenuItem("Additive"u8))
                            ThreadHelper.ExecuteOnMainThread(() => VoxelRuntime.Instance.SceneManager.CreateScene("Unititled", LoadSceneMode.Additive));

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
                    IterateNodeRecursive(runtime, child);
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

                Scene scene = runtime.SceneManager.LoadScene(localPath, loadMode);
                runtime.EditorManager.SceneTracker.UpdateScenePath(scene, localPath);
            }
        }

        private async Task TrySaveScene(VoxelRuntime runtime, Scene? scene, bool overwriteIfExists)
        {
            if (scene != null)
            {
                string? sourcePath = null;
                if (!overwriteIfExists || !runtime.EditorManager.SceneTracker.TryGetScenePath(scene, out sourcePath))
                {
                    Window? window = runtime.WindowManager.PrimaryWindow;

                    SaveFileDialogResult dialogResult = await Task.Factory.StartNew(() => FileDialog.SaveFile(new SaveFileDialogParams
                    {
                        DefaultDirectory = ProjectData.Instance!.Paths.ContentFolder,
                        Filters = [new FileFilter("Scene files", "*.scene")],
                        DefaultFileName = sourcePath,
                    }, window));

                    if (dialogResult.Result == FileDialogResult.Ok && FilesystemManager.TryGetLocalPath(dialogResult.File, out sourcePath))
                    {
                        sourcePath = Path.ChangeExtension(sourcePath, ".scene");
                        runtime.EditorManager.SceneTracker.UpdateScenePath(scene, sourcePath);

                        scene.Name = Path.GetFileNameWithoutExtension(sourcePath) ?? "Untitled";
                    }
                    else
                    {
                        return;
                    }
                }

                try
                {
                    if (FilesystemManager.TryGetFullPath(sourcePath, out string? fullPath))
                    {

                        string tempPath = Path.Combine(ProjectData.Instance!.Paths.LibraryFolder, "tempScene.scene");

                        {
                            using Stream stream = FileUtility.TryWaitOpen(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, maxTries: 3, timeoutMs: 50);
                            SceneSerializer.Serialize(stream, scene);
                        }

                        File.Move(tempPath, fullPath, true);
                    }
                    else
                    {
                        AppLog.Editor.Error("Failed to get full path to serialize scene '{scn}' ({path})", scene.Name, sourcePath);
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Editor.Error(ex, "Failed to serialize scene '{scn}' ({path})", scene.Name, sourcePath);
                }
            }
            else
            {
                foreach (Scene loadedScene in runtime.SceneManager.Scenes)
                {
                    await TrySaveScene(runtime, scene, overwriteIfExists);
                }
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

            if (isArrowHovered || isHeaderHovered)
            {
                _currentHoveredEntity = entity;
            }

            if (Select.IsSelected(entity))
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

            return isOpened;
        }

        private static string GetSelectionAsString(SceneEntity? additional)
        {
            try
            {
                using RentedList<SceneEntity> entities = new RentedList<SceneEntity>();
                foreach (object obj in Select.Objects)
                {
                    if (obj is SceneEntity entity)
                        entities.Add(entity);
                }

                if (additional.HasValue && !entities.Contains(additional.Value))
                    entities.Add(additional.Value);

                using PooledMemoryStream stream = new PooledMemoryStream();
                EntitySerializer.Serialize(stream, entities.AsSpan());

                return Encoding.UTF8.GetString(stream.AsSpan());
            }
            catch (Exception ex)
            {
                AppLog.Editor.Warning(ex, "Failed serialize entity to string");
            }

            return string.Empty;
        }

        private static void PasteFromString(string src, SceneEntity parentEntity)
        {
            byte[] clipboardBytes = Encoding.UTF8.GetBytes(src);

            Scene scene = parentEntity.Scene;
            try
            {
                using MemoryStream stream = new MemoryStream(clipboardBytes, false);
                SceneEntity newEntity = EntitySerializer.Deserialize(stream, scene, parentEntity);

                Select.Add(newEntity, SelectMode.Clear);
            }
            catch (Exception ex)
            {
                AppLog.Editor.Warning(ex, "Failed to paste entity from string");
            }
        }

        private static void AddSelectionToClipboard(SceneEntity? additional)
        {
            try
            {
                using RentedList<SceneEntity> entities = new RentedList<SceneEntity>();
                foreach (object obj in Select.Objects)
                {
                    if (obj is SceneEntity entity)
                        entities.Add(entity);
                }

                if (additional.HasValue && !entities.Contains(additional.Value))
                    entities.Add(additional.Value);

                using PooledMemoryStream stream = new PooledMemoryStream();
                EntitySerializer.Serialize(stream, entities.AsSpan());

                Clipboard.SetText(stream.AsSpan());
            }
            catch (Exception ex)
            {
                AppLog.Editor.Warning(ex, "Failed to copy entity to clipboard");
            }
        }

        private static void PasteFromClipboard(SceneEntity parentEntity)
        {
            if (Clipboard.HasData())
            {
                byte[]? clipboardBytes = Clipboard.GetBytes();
                if (clipboardBytes == null)
                    return;

                Scene scene = parentEntity.Scene;
                try
                {
                    using MemoryStream stream = new MemoryStream(clipboardBytes, false);
                    SceneEntity newEntity = EntitySerializer.Deserialize(stream, scene, parentEntity);

                    Select.Add(newEntity, SelectMode.Clear);
                }
                catch (Exception ex)
                {
                    AppLog.Editor.Warning(ex, "Failed to paste entity from clipboard");
                }
            }
        }
    }
}
