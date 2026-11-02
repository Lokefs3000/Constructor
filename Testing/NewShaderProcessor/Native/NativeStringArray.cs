using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NewShaderProcessor.Native
{
    internal unsafe struct NativeStringArray : IDisposable
    {
        private readonly char** _array;
        private readonly int _length;

        public NativeStringArray(params string[] elements)
        {
            _length = elements.Length;

            _array = (char**)NativeMemory.Alloc((nuint)(elements.Length + 1), (nuint)Unsafe.SizeOf<nint>());
            _array[elements.Length] = null;

            for (int i = 0; i < elements.Length; ++i)
            {
                string str = elements[i];

                char* ptr = _array[i];
                ptr = (char*)NativeMemory.Alloc((nuint)(str.Length + 1), (nuint)Unsafe.SizeOf<char>());

                for (int j = 0; j < str.Length; ++j)
                {
                    ptr[j] = str[j];
                }

                ptr[str.Length] = '\0';
            }
        }

        public readonly void Dispose()
        {
            int i = 0;
            while (_array[i] != null)
            {
                NativeMemory.Free(_array[i]);
                ++i;
            }

            NativeMemory.Free(_array);
        }

        public readonly char** Array => _array;
        public readonly int Length => _length;
    }
}
