using System;
using System.Collections.Generic;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Components;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Scenes;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Rendering;
using VoxelizationDemo.Editor.Utility;

namespace VoxelizationDemo.Editor
{
    internal sealed class ViewportManager
    {
        internal ViewportManager()
        {

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
        }
    }
}
