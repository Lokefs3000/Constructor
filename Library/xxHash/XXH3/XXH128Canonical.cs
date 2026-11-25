using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace xxHash.XXH3
{
    public unsafe struct XXH128Canonical
    {
        public fixed byte Digest[128 / 8];
    }
}
