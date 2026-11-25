using System;
using System.Collections.Generic;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Components;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Rendering;
using Primary.Scenes;
using Primary.Threading;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Input;
using VoxelizationDemo.Editor.Utility;
using VoxelizationDemo.Editor.Viewports;
using VoxelizationDemo.Editor.Windows;

namespace VoxelizationDemo.Editor
{
    internal sealed class ViewportManager
    {
        private readonly KeybindContext _keybindContext;
        private readonly CameraManager _cameraManager;

        private bool _openViewMenu;

        internal ViewportManager()
        {
            _keybindContext = new KeybindContext();
            _keybindContext.DeclareBinding("Open view menu", OnViewKeybindPressed, KeyModifier.Shift, KeyCode.V);

            _cameraManager = new CameraManager();

            ThreadHelper.ExecuteOnMainThread(() => EditorManager.Instance.KeybindManager.FocusContext(_keybindContext));
        }

        private void OnViewKeybindPressed() => _openViewMenu = true;

        internal void Enable()
        {
            RenderingManager manager = VoxelRuntime.Instance.RenderingManager;
            manager.RenderWorld.TransformOutput.Subscribe(OnTransformOutput);
        }

        internal void Disable()
        {
            RenderingManager manager = VoxelRuntime.Instance.RenderingManager;
            manager.RenderWorld.TransformOutput.Unsubscribe(OnTransformOutput);
        }

        private void OnTransformOutput(List<RenderOutputData> outputData)
        {
            outputData.Clear();
            _cameraManager.SubmitOutput(outputData);
        }

        internal void UpdateViewport()
        {
            EditorState state = VoxelRuntime.Instance.EditorManager.EditorState;

            if (!ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow | ImGuiHoveredFlags.AllowWhenBlockedByPopup) && !ImGui.IsAnyItemActive() && !ImGui.IsAnyItemHovered())
            {
                PointerDevice pointer = InputSystem.Pointer;
                KeyboardDevice keyboard = InputSystem.Keyboard;

                if (keyboard.KeyModifiers.HasAny(KeyModifier.Shift))
                {
                    if (state.PrimaryScene != null && !pointer.IsButtonHeld(MouseButton.Right) && keyboard.IsKeyReleased(KeyCode.A))
                    {
                        ImGui.OpenPopup("ADD_NEW"u8);
                    }
                }
            }

            if (ImGui.BeginPopup("ADD_NEW"u8))
            {
                if (state.PrimaryScene != null)
                {
                    ImGui.Text(state.PrimaryScene.Name);
                    ImGui.Indent();

                    if (ImGui.MenuItem("Blank entity"u8))
                    {
                        SceneEntity entity = state.PrimaryScene.CreateEntity(SceneEntity.Null);

                        Select.Add(entity);
                    }

                    if (ImGui.MenuItem("Mesh renderer"u8))
                    {
                        SceneEntity entity = state.PrimaryScene.CreateEntity(SceneEntity.Null);
                        entity.AddComponent<MeshRenderer>();

                        Select.Add(entity);
                    }

                    ImGui.Unindent();
                }

                ImGui.EndPopup();
            }

            if (_openViewMenu)
            {
                ImGui.OpenPopup("VIEW_MENU"u8);
                _openViewMenu = false;
            }

            if (ImGui.BeginPopup("VIEW_MENU"u8))
            {
                if (ImGui.MenuItem("Profiler window"u8))
                {
                    if (!EditorManager.Instance.WindowManager.HasWindowOpen<ProfilerWindow>())
                    {
                        EditorManager.Instance.WindowManager.AddOpenWindow(new ProfilerWindow());
                    }
                }

                if (ImGui.MenuItem("Render inspector"u8))
                {
                    if (!EditorManager.Instance.WindowManager.HasWindowOpen<RenderInspector>())
                    {
                        EditorManager.Instance.WindowManager.AddOpenWindow(new RenderInspector());
                    }
                }

                if (ImGui.MenuItem("Assemblies"u8))
                {
                    if (!EditorManager.Instance.WindowManager.HasWindowOpen<AssemblyWindow>())
                    {
                        EditorManager.Instance.WindowManager.AddOpenWindow(new AssemblyWindow());
                    }
                }

                if (ImGui.MenuItem("Component registry"u8))
                {
                    if (!EditorManager.Instance.WindowManager.HasWindowOpen<ComponentRegistryWindow>())
                    {
                        EditorManager.Instance.WindowManager.AddOpenWindow(new ComponentRegistryWindow());
                    }
                }

                ImGui.EndPopup();
            }

            _cameraManager.UpdateCamera();
        }

        public CameraManager Camera => _cameraManager;
    }
}
