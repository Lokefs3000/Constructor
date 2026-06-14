using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace EditorUI.Reflection.Dynamic
{
    public record class PropertyMethods(Delegate SetDirect)
    {
        public SetFieldDirect<T> GetSetFieldDirectUnsafe<T>()
        {
            Debug.Assert(SetDirect is SetFieldDirect<T>);
            return Unsafe.As<SetFieldDirect<T>>(SetDirect);
        }

        public SetPropertyDirect<T> GetSetPropertyDirectUnsafe<T>()
        {
            Debug.Assert(SetDirect is SetPropertyDirect<T>);
            return Unsafe.As<SetPropertyDirect<T>>(SetDirect);
        }
    }

    public delegate void SetFieldDirect<T>(object instance, ref readonly T value);
    public delegate void SetPropertyDirect<T>(object instance, ref readonly T value);
}
