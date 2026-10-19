using System;
using System.Collections.Generic;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Components;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Scenes;
using Primary.Threading;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Input;
using VoxelizationDemo.Editor.Utility;
using VoxelizationDemo.Editor.Windows;

namespace VoxelizationDemo.Editor
{
    internal sealed class ViewportManager
    {
        private readonly KeybindContext _keybindContext;

        private bool _openViewMenu;

        internal ViewportManager()
        {
            _keybindContext = new KeybindContext();
            _keybindContext.DeclareBinding("Open view menu", OnViewKeybindPressed, KeyModifier.Shift, KeyCode.V);

            ThreadHelper.ExecuteOnMainThread(() => EditorManager.Instance.KeybindManager.FocusContext(_keybindContext));
        }

        private void OnViewKeybindPressed() => _openViewMenu = true;

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

                ImGui.EndPopup();
            }
        }
    }
}
