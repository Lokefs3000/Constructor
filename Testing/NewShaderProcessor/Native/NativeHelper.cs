using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NewShaderProcessor.Native
{
    internal unsafe sealed class NativeHelper
    {
        internal static T* New<T>() where T : unmanaged
        {
            return (T*)NativeMemory.Alloc((nuint)Unsafe.SizeOf<T>());
        }

        internal static void Free<T>(T* ptr) where T : unmanaged
        {
            if (ptr != null)
            {
                NativeMemory.Free(ptr);
            }
        }
    }
}
