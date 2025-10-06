using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Common
{
    public static class Checking
    {
        [StackTraceHidden]
        public static void Assert([DoesNotReturnIf(false)] bool condition, in string message) //should be inlined
        {
            if (!condition)
                throw new Exception(message);
        }
    }
}
