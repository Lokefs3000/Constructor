using System.Runtime.CompilerServices;

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
