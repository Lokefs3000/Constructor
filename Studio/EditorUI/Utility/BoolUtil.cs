using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace EditorUI.Utility
{
    internal static class BoolUtil<T>
    {
        internal static bool GetAsBoolean(ref T value)
        {
            return Unsafe.As<T, bool>(ref value);
        }
    }
}
