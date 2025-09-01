using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Common
{
    public static class ExceptionUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool ret, string? message = null)
        {
            if (!ret)
                throw new Exception(message);
        }
    }
}
