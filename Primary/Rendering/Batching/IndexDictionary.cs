using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Primary.Rendering.Batching
{
    // based on https://github.com/microsoft/referencesource/blob/main/mscorlib/system/collections/generic/dictionary.cs
    internal sealed class IndexDictionary
    {
        private Entry[] _nodes;
        private int[] _buckets;

        private int _count;
        private int _freeCount;

        private uint _fastModMultiplier;

        internal IndexDictionary()
        {
            _nodes = [];
            _buckets = [];

            _count = 0;
            _freeCount = 0;

            _fastModMultiplier = 0;
        }

        public void GetOrAdd()
        {

        }

        private struct Entry
        {

        }

        // based on https://github.com/dotnet/runtime/issues/113352#issuecomment-2713941980
        private static uint FastMod(uint value, uint divisor, uint multiplier)
        {
            if (Bmi2.X64.IsSupported)
                return (uint)Bmi2.X64.MultiplyNoFlags(multiplier * value, divisor);
            else
                return (uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);
        }
    }
}
