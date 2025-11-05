using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Common
{
    public static class Checking
    {
        [StackTraceHidden]
        public static void Assert([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression(nameof(condition))] in string? message = null) //should be inlined
        {
            if (!condition)
                throw new Exception(message);
        }
    }
}
