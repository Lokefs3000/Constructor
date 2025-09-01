using System.Runtime.InteropServices;

namespace Primary.Interop
{
    public static unsafe partial class D3D12MA
    {
        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?CreateAllocator@D3D12MA@@YAJPEBUALLOCATOR_DESC@1@PEAPEAVAllocator@1@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateAllocator([NativeTypeName("const ALLOCATOR_DESC *")] ALLOCATOR_DESC* pDesc, Allocator** ppAllocator);

        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "?CreateVirtualBlock@D3D12MA@@YAJPEBUVIRTUAL_BLOCK_DESC@1@PEAPEAVVirtualBlock@1@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateVirtualBlock([NativeTypeName("const VIRTUAL_BLOCK_DESC *")] VIRTUAL_BLOCK_DESC* pDesc, VirtualBlock** ppVirtualBlock);

        [return: NativeTypeName("D3D12MA::ALLOCATION_FLAGS")]
        public static ALLOCATION_FLAGS Or([NativeTypeName("D3D12MA::ALLOCATION_FLAGS")] ALLOCATION_FLAGS a, [NativeTypeName("D3D12MA::ALLOCATION_FLAGS")] ALLOCATION_FLAGS b)
        {
            return (ALLOCATION_FLAGS)(((int)(a)) | ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::ALLOCATION_FLAGS")]
        public static ALLOCATION_FLAGS And([NativeTypeName("D3D12MA::ALLOCATION_FLAGS")] ALLOCATION_FLAGS a, [NativeTypeName("D3D12MA::ALLOCATION_FLAGS")] ALLOCATION_FLAGS b)
        {
            return (ALLOCATION_FLAGS)(((int)(a)) & ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::ALLOCATION_FLAGS")]
        public static ALLOCATION_FLAGS OnesComplement([NativeTypeName("D3D12MA::ALLOCATION_FLAGS")] ALLOCATION_FLAGS a)
        {
            return (ALLOCATION_FLAGS)(~((int)(a)));
        }

        [return: NativeTypeName("D3D12MA::ALLOCATION_FLAGS")]
        public static ALLOCATION_FLAGS Xor([NativeTypeName("D3D12MA::ALLOCATION_FLAGS")] ALLOCATION_FLAGS a, [NativeTypeName("D3D12MA::ALLOCATION_FLAGS")] ALLOCATION_FLAGS b)
        {
            return (ALLOCATION_FLAGS)(((int)(a)) ^ ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")]
        public static DEFRAGMENTATION_FLAGS Or([NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")] DEFRAGMENTATION_FLAGS a, [NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")] DEFRAGMENTATION_FLAGS b)
        {
            return (DEFRAGMENTATION_FLAGS)(((int)(a)) | ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")]
        public static DEFRAGMENTATION_FLAGS And([NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")] DEFRAGMENTATION_FLAGS a, [NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")] DEFRAGMENTATION_FLAGS b)
        {
            return (DEFRAGMENTATION_FLAGS)(((int)(a)) & ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")]
        public static DEFRAGMENTATION_FLAGS OnesComplement([NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")] DEFRAGMENTATION_FLAGS a)
        {
            return (DEFRAGMENTATION_FLAGS)(~((int)(a)));
        }

        [return: NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")]
        public static DEFRAGMENTATION_FLAGS Xor([NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")] DEFRAGMENTATION_FLAGS a, [NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")] DEFRAGMENTATION_FLAGS b)
        {
            return (DEFRAGMENTATION_FLAGS)(((int)(a)) ^ ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")]
        public static ALLOCATOR_FLAGS Or([NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")] ALLOCATOR_FLAGS a, [NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")] ALLOCATOR_FLAGS b)
        {
            return (ALLOCATOR_FLAGS)(((int)(a)) | ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")]
        public static ALLOCATOR_FLAGS And([NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")] ALLOCATOR_FLAGS a, [NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")] ALLOCATOR_FLAGS b)
        {
            return (ALLOCATOR_FLAGS)(((int)(a)) & ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")]
        public static ALLOCATOR_FLAGS OnesComplement([NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")] ALLOCATOR_FLAGS a)
        {
            return (ALLOCATOR_FLAGS)(~((int)(a)));
        }

        [return: NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")]
        public static ALLOCATOR_FLAGS Xor([NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")] ALLOCATOR_FLAGS a, [NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")] ALLOCATOR_FLAGS b)
        {
            return (ALLOCATOR_FLAGS)(((int)(a)) ^ ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::POOL_FLAGS")]
        public static POOL_FLAGS Or([NativeTypeName("D3D12MA::POOL_FLAGS")] POOL_FLAGS a, [NativeTypeName("D3D12MA::POOL_FLAGS")] POOL_FLAGS b)
        {
            return (POOL_FLAGS)(((int)(a)) | ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::POOL_FLAGS")]
        public static POOL_FLAGS And([NativeTypeName("D3D12MA::POOL_FLAGS")] POOL_FLAGS a, [NativeTypeName("D3D12MA::POOL_FLAGS")] POOL_FLAGS b)
        {
            return (POOL_FLAGS)(((int)(a)) & ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::POOL_FLAGS")]
        public static POOL_FLAGS OnesComplement([NativeTypeName("D3D12MA::POOL_FLAGS")] POOL_FLAGS a)
        {
            return (POOL_FLAGS)(~((int)(a)));
        }

        [return: NativeTypeName("D3D12MA::POOL_FLAGS")]
        public static POOL_FLAGS Xor([NativeTypeName("D3D12MA::POOL_FLAGS")] POOL_FLAGS a, [NativeTypeName("D3D12MA::POOL_FLAGS")] POOL_FLAGS b)
        {
            return (POOL_FLAGS)(((int)(a)) ^ ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")]
        public static VIRTUAL_BLOCK_FLAGS Or([NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")] VIRTUAL_BLOCK_FLAGS a, [NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")] VIRTUAL_BLOCK_FLAGS b)
        {
            return (VIRTUAL_BLOCK_FLAGS)(((int)(a)) | ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")]
        public static VIRTUAL_BLOCK_FLAGS And([NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")] VIRTUAL_BLOCK_FLAGS a, [NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")] VIRTUAL_BLOCK_FLAGS b)
        {
            return (VIRTUAL_BLOCK_FLAGS)(((int)(a)) & ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")]
        public static VIRTUAL_BLOCK_FLAGS OnesComplement([NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")] VIRTUAL_BLOCK_FLAGS a)
        {
            return (VIRTUAL_BLOCK_FLAGS)(~((int)(a)));
        }

        [return: NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")]
        public static VIRTUAL_BLOCK_FLAGS Xor([NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")] VIRTUAL_BLOCK_FLAGS a, [NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")] VIRTUAL_BLOCK_FLAGS b)
        {
            return (VIRTUAL_BLOCK_FLAGS)(((int)(a)) ^ ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")]
        public static VIRTUAL_ALLOCATION_FLAGS Or([NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")] VIRTUAL_ALLOCATION_FLAGS a, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")] VIRTUAL_ALLOCATION_FLAGS b)
        {
            return (VIRTUAL_ALLOCATION_FLAGS)(((int)(a)) | ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")]
        public static VIRTUAL_ALLOCATION_FLAGS And([NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")] VIRTUAL_ALLOCATION_FLAGS a, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")] VIRTUAL_ALLOCATION_FLAGS b)
        {
            return (VIRTUAL_ALLOCATION_FLAGS)(((int)(a)) & ((int)(b)));
        }

        [return: NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")]
        public static VIRTUAL_ALLOCATION_FLAGS OnesComplement([NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")] VIRTUAL_ALLOCATION_FLAGS a)
        {
            return (VIRTUAL_ALLOCATION_FLAGS)(~((int)(a)));
        }

        [return: NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")]
        public static VIRTUAL_ALLOCATION_FLAGS Xor([NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")] VIRTUAL_ALLOCATION_FLAGS a, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")] VIRTUAL_ALLOCATION_FLAGS b)
        {
            return (VIRTUAL_ALLOCATION_FLAGS)(((int)(a)) ^ ((int)(b)));
        }
    }
}
