using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace EditorUI.Reflection.Dynamic
{
    public record class PropertyMethods(Delegate? SetDirect, SetIndirect? SetIndirect)
    {
        public SetDirect<T>? GetSetDirect<T>()
        {
            Debug.Assert(SetDirect is SetDirect<T>);
            return Unsafe.As<SetDirect<T>?>(SetDirect);
        }
    }

    public delegate void SetDirect<T>(object instance, T value);
    public delegate void SetIndirect(object instance, object? value);
}
