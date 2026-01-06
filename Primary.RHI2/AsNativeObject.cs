using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.RHI2
{
    public unsafe interface AsNativeObject<T> where T : unmanaged
    {
        public T* GetAsNative();
    }
}
