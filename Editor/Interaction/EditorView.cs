using Editor.History;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Profiling;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Interaction
{
    public sealed class EditorView
    {
        private Vector2 _viewSize;
        private Vector2 _mousePosition;

        private MouseButtonData[] _buttons;

        internal EditorView()
        {
            _viewSize = Vector2.Zero;
            _mousePosition = Vector2.Zero;

            _buttons = new MouseButtonData[3];
        }

        internal void UpdateState()
        {
            using (new ProfilingScope("EditorView"))
            {
                for (int i = 0; i < _buttons.Length; i++)
                {
                    _buttons[i] = Flags.RemoveFlags(_buttons[i], MouseButtonData.Updated);
                }

                if (InputSystem.Keyboard.IsKeyReleased(KeyCode.Z) && Flags.HasEither(InputSystem.Keyboard.KeyModifiers, KeyModifier.Control))
                    HistoryManager.Undo();
                if (InputSystem.Keyboard.IsKeyReleased(KeyCode.Y) && Flags.HasEither(InputSystem.Keyboard.KeyModifiers, KeyModifier.Control))
                    HistoryManager.Redo();
            }
        }

        internal void UpdateButtonState(MouseButton button, bool isHeld)
        {
            if (isHeld)
                _buttons[(int)button] = MouseButtonData.Held | MouseButtonData.Updated;
            else
                _buttons[(int)button] = MouseButtonData.Updated;
        }

        public bool IsButtonPressed(MouseButton button) => _buttons[(int)button] == (MouseButtonData.Held | MouseButtonData.Updated);
        public bool IsButtonReleased(MouseButton button) => _buttons[(int)button] == MouseButtonData.Updated;

        public bool IsButtonHeld(MouseButton button) => Flags.HasFlag(_buttons[(int)button], MouseButtonData.Held);

        public Vector2 ViewSize { get => _viewSize; internal set => _viewSize = value; }
        public Vector2 MousePosition { get => _mousePosition; internal set => _mousePosition = value; }

        public static EditorView Instance => EditorRuntime.GlobalSingleton.EditorView;

        private enum MouseButtonData : byte
        {
            None = 0,

            Held = 1 << 0,
            Updated = 1 << 1
        }
    }
}
