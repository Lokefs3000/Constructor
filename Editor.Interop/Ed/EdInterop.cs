using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Editor.Interop.Ed
{
    public static unsafe partial class EdInterop
    {
        private const string LibraryName = "edinterop.dll";

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial nint SPIRV_CreateOptimize();

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SPIRV_DestroyOptimize(nint optimizer);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SPIRV_OptRegisterPerfPasses(nint optimizer);

        [LibraryImport(LibraryName)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool SPIRV_RunOptimize(nint optimizer, SPIRV_OptimizeOut* @out);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct SPIRV_OptimizeOut
    {
        public delegate* unmanaged[Cdecl]<ulong, void*> Alloc;

        public uint* InBinary;
        public ulong InSize;

        public uint* OutBinary;
        public ulong OutSize;
    };
}
