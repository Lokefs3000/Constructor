using System;
using System.Collections.Generic;
using System.Text;
using Hexa.NET.ImGui;
using Primary.Collections.ReadOnly;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Scenes;
using Primary.Threading;
using Primary.Utility;
using VoxelizationDemo.Editor.Utility;

namespace VoxelizationDemo.Editor.Tools
{
    internal sealed class ToolManager
    {
        private readonly List<EntityToolTransform> _transforms;
        private readonly List<Tool> _tools;

        private Tool? _activeTool;

        private ToolOriginMode _originMode;
        private ToolRelativeMode _relativeMode;

        internal ToolManager()
        {
            _transforms = new List<EntityToolTransform>();
            _tools = new List<Tool>();

            _activeTool = null;

            _originMode = ToolOriginMode.Centered;
            _relativeMode = ToolRelativeMode.Local;

            ThreadHelper.ExecuteOnMainThread(() =>
            {
                Select.OnObjectSelected += OnObjectSelectedCallback;
                Select.OnObjectDeselected += OnObjectDeselectedCallback;
            });
        }

        private void OnObjectSelectedCallback(object obj)
        {
            if (obj is SceneEntity entity)
            {
                _transforms.Add(new EntityToolTransform(entity));
            }
        }

        private void OnObjectDeselectedCallback(object obj)
        {
            if (obj is SceneEntity entity)
            {
                _transforms.RemoveWhere((x) => x.Entity == entity);
            }
        }

        internal void UpdateTools()
        {
            if (_activeTool != null)
            {
                _activeTool.Update();

                if (_activeTool.IsBusy)
                {
                    if (!InputSystem.Pointer.IsButtonHeld(MouseButton.Left))
                    {
                        _activeTool.Finish();
                    }
                    else if (InputSystem.Keyboard.IsKeyReleased(KeyCode.Escape))
                    {
                        _activeTool.Cancel();
                    }
                }
                else if (_transforms.Count > 0)
                {
                    ImGuiIOPtr io = ImGui.GetIO();
                    if (!io.WantCaptureMouse && InputSystem.Pointer.IsButtonPressed(MouseButton.Left))
                    {
                        _activeTool.TryUseTool();
                    }
                }
            }
        }

        public void ClearTool()
        {
            if (_activeTool == null || _activeTool.IsBusy)
                return;

            _activeTool?.DeactivateSelf();
            _activeTool = null;
        }

        public void ChangeTool<T>() where T : Tool, new()
        {
            if (_activeTool is T || (_activeTool != null && _activeTool.IsBusy))
                return;

            T? newTool = null;
            foreach (Tool tool in _tools)
            {
                if (tool is T castType)
                {
                    newTool = castType;
                    break;
                }
            }

            if (newTool == null)
            {
                newTool = new T();
                _tools.Add(newTool);
            }

            _activeTool?.DeactivateSelf();
            newTool.ActiveSelf();

            _activeTool = newTool;
        }

        internal ROList<EntityToolTransform> Transforms => _transforms;

        public Tool? ActiveTool => _activeTool;

        public ToolOriginMode OriginMode { get => _originMode; set => _originMode = value; }
        public ToolRelativeMode RelativeMode { get => _relativeMode; set => _relativeMode = value; }
    }

    public enum ToolOriginMode : byte
    {
        Centered = 0,
        First,
        Last
    }

    public enum ToolRelativeMode : byte
    {
        Local = 0,
        World
    }
}
