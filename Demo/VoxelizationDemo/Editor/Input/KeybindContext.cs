using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;

namespace VoxelizationDemo.Editor.Input
{
    internal sealed class KeybindContext
    {
        private readonly Dictionary<KeyCode, KeybindData[]> _bindings;

        internal KeybindContext()
        {
            _bindings = new Dictionary<KeyCode, KeybindData[]>();
        }

        internal bool TryInvokeBinding(KeyCode key, KeyModifier modifiers)
        {
            if (_bindings.TryGetValue(key, out KeybindData[]? binds))
            {
                foreach (KeybindData keybind in binds)
                {
                    if (keybind.Modifiers == KeyModifier.None || modifiers.HasAny(keybind.Modifiers))
                    {
                        foreach (KeyCode requiredKey in keybind.Keys)
                        {
                            if (requiredKey != key && !InputSystem.Keyboard.IsKeyDown(requiredKey))
                                goto TryNextBind;
                        }

                        keybind.Binding();
                        return true;
                    }

                TryNextBind:
                    continue;
                }
            }

            return false;
        }

        internal void DeclareBinding(string name, KeybindDelegate @delegate, params KeyCode[] keys)
            => DeclareBinding(name, @delegate, KeyModifier.None, keys);

        internal void DeclareBinding(string name, KeybindDelegate @delegate, KeyModifier modifiers, params KeyCode[] keys)
        {
            KeybindData keybind = new KeybindData(name, [.. keys], modifiers, @delegate);
            foreach (KeyCode key in keybind.Keys)
            {
                ref KeybindData[]? bindsForKey = ref CollectionsMarshal.GetValueRefOrAddDefault(_bindings, key, out bool exists);
                if (!exists || bindsForKey == null)
                {
                    bindsForKey = [keybind];
                }
                else
                {
                    Array.Resize(ref bindsForKey, bindsForKey.Length + 1);
                    bindsForKey[^1] = keybind;
                }
            }
        }

        private readonly record struct KeybindData(string Name, FrozenSet<KeyCode> Keys, KeyModifier Modifiers, KeybindDelegate Binding);
    }

    internal delegate void KeybindDelegate();
}
