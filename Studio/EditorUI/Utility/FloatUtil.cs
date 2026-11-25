using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Utility
{
    internal static class FloatUtil
    {
        // https://stackoverflow.com/questions/639010/how-can-i-compare-a-float-to-nan-if-comparisons-to-nan-always-return-false
        public static unsafe bool IsFloatNanBitwise(float f)
        {
            int binary = *(int*)(&f);
            return ((binary & 0x7F800000) == 0x7F800000) && ((binary & 0x007FFFFF) != 0);
        }
    }
}
