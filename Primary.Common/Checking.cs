using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Primary.Common
{
    public static class Checking
    {
        [StackTraceHidden]
        public static void Assert([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression(nameof(condition))] in string? message = null) //should be inlined
        {
            if (!condition)
                throw new AssertFailedException(message);
        }

        [StackTraceHidden]
        public static void FatalAssert([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression(nameof(condition))] in string? message = null) //should be inlined
        {
            if (!condition)
                throw new AssertFailedException(message);
        }
    }

    public class AssertFailedException : Exception
    {
        public AssertFailedException()
        {
        }

        public AssertFailedException(string? message) : base(message)
        {
        }

        public AssertFailedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected AssertFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
