using System;
using System.Collections.Generic;
using System.Text;
using Primary.Input;
using Primary.Input.Devices;
using VoxelizationDemo.Editor.Input;

namespace VoxelizationDemo.Editor
{
    internal sealed class KeybindManager : IDisposable
    {
        // TODO: Use a custom array instead to avoid an copy
        private readonly List<KeybindContext> _contexts;

        private bool _disposedValue;

        internal KeybindManager()
        {
            _contexts = new List<KeybindContext>();

            InputSystem.Keyboard.KeyPressed += OnKeyPressed;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    InputSystem.Keyboard.KeyPressed -= OnKeyPressed;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void OnKeyPressed(KeyCode key)
        {
            foreach (KeybindContext context in _contexts)
            {
                if (context.TryInvokeBinding(key, InputSystem.Keyboard.KeyModifiers))
                {
                    return;
                }
            }
        }

        internal void FocusContext(KeybindContext context)
        {
            if (_contexts.Count > 0)
            {
                if (_contexts[0] == context)
                {
                    return;
                }
                else if (_contexts.Count > 1 && _contexts[1] == context)
                {
                    (_contexts[0], _contexts[1]) = (_contexts[1], _contexts[0]);
                    return;
                }

                _contexts.Remove(context);
                _contexts.Insert(0, context);
            }
            else
            {
                _contexts.Add(context);
            }
        }

        internal void RemoveContext(KeybindContext context)
        {
            _contexts.Remove(context);
        }
    }
}
