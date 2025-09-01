using System.Runtime.InteropServices;

namespace Primary.Interop
{
    public static partial class PIX
    {
        [LibraryImport("WinPixEventRuntime.dll", EntryPoint = "PIXBeginEventOnCommandList", StringMarshalling = StringMarshalling.Utf8)]
        public static partial void PIXBeginEventOnCommandList(nint commandList, ulong color, string formatString);

        [LibraryImport("WinPixEventRuntime.dll", EntryPoint = "PIXEndEventOnCommandList")]
        public static partial void PIXEndEventOnCommandList(nint commandList);

        [LibraryImport("WinPixEventRuntime.dll", EntryPoint = "PIXSetMarkerOnCommandList", StringMarshalling = StringMarshalling.Utf8)]
        public static partial void PIXSetMarkerOnCommandList(nint commandList, ulong color, string formatString);
    }
}