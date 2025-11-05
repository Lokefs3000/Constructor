using System.Runtime.InteropServices;

namespace Interop.D3D12MemAlloc
{
    public static unsafe partial class D3D12MA
    {
        [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?CreateAllocator@D3D12MA@@YAJPEBUALLOCATOR_DESC@1@PEAPEAVAllocator@1@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateAllocator([NativeTypeName("const ALLOCATOR_DESC *")] ALLOCATOR_DESC* pDesc, Allocator** ppAllocator);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?CreateVirtualBlock@D3D12MA@@YAJPEBUVIRTUAL_BLOCK_DESC@1@PEAPEAVVirtualBlock@1@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateVirtualBlock([NativeTypeName("const VIRTUAL_BLOCK_DESC *")] VIRTUAL_BLOCK_DESC* pDesc, VirtualBlock** ppVirtualBlock);
    }
}
