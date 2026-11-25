using System;
using System.Collections.Generic;
using System.Text;
using xxHash.Int64;

namespace xxHash.Int64
{
    public static unsafe class XXHash64
    {
        public static uint XXH64(void* input, ulong length, ulong seed)
        {

        }

        public static XXH64State* CreateState()
        {

        }

        public static XXHErrorCode FreeState(XXH64State* state)
        {

        }

        public static XXH64State* CopyState(XXH64State* dstState, XXH64State* srcState)
        {

        }

        public static XXHErrorCode Reset(XXH64State* state, ulong seed)
        {

        }

        public static XXHErrorCode Update(XXH64State* state, void* input, ulong length)
        {

        }

        public static ulong Digest(XXH64State* state)
        {

        }

        public static void CanonicalFromHash(XXH64Canonical* dst, ulong hash)
        {

        }

        public static ulong HashFromCanonical(XXH64Canonical* dst)
        {

        }
    }
}
