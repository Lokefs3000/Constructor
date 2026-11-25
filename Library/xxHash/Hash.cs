using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace xxHash
{
    public static class Hash
    {
        public const int VersionMajor = 0;
        public const int VersionMinor = 8;
        public const int VersionRelease = 3;

        public const int VersionNumber = VersionMajor * 100 * 100 + VersionMinor * 100 + VersionRelease;

        public const int ForceMemoryAccess = 0;
        // this should be evaluated at JIT time to a constant
        public static bool ForceAlignCheck = 
            RuntimeInformation.ProcessArchitecture == Architecture.X86 ||
            RuntimeInformation.ProcessArchitecture == Architecture.X64 ||
            RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
        public const int NoInlineHints = 0;

        public const int XXH3InlineSecret = 1;

        public const bool XXH32EndJump = 0;
    }
}
