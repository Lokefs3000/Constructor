using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;

namespace Primary.Interop
{
    [NativeTypeName("struct Allocation : D3D12MA::IUnknownImpl")]
    public unsafe partial struct Allocation
    {
        public IUnknownImpl Base;

        [NativeTypeName("D3D12MA::AllocatorPimpl *")]
        private AllocatorPimpl* m_Allocator;

        [NativeTypeName("UINT64")]
        private ulong m_Size;

        [NativeTypeName("UINT64")]
        private ulong m_Alignment;

        private void* m_Resource;

        private void* m_pPrivateData;

        [NativeTypeName("wchar_t *")]
        private ushort* m_Name;

        [NativeTypeName("__AnonymousRecord_D3D12MemAlloc_L621_C5")]
        private _Anonymous_e__Union Anonymous;

        [NativeTypeName("struct PackedData")]
        private PackedData m_PackedData;

        [UnscopedRef]
        private ref _Anonymous_e__Union._m_Committed_e__Struct m_Committed
        {
            get
            {
                return ref Anonymous.m_Committed;
            }
        }

        [UnscopedRef]
        private ref _Anonymous_e__Union._m_Placed_e__Struct m_Placed
        {
            get
            {
                return ref Anonymous.m_Placed;
            }
        }

        [UnscopedRef]
        private ref _Anonymous_e__Union._m_Heap_e__Struct m_Heap
        {
            get
            {
                return ref Anonymous.m_Heap;
            }
        }

        //[DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "??0Allocation@D3D12MA@@AEAA@PEAVAllocatorPimpl@1@_K1@Z", ExactSpelling = true)]
        //private static extern Allocation(Allocation* pThis, [NativeTypeName("D3D12MA::AllocatorPimpl *")] AllocatorPimpl* allocator, [NativeTypeName("UINT64")] ulong size, [NativeTypeName("UINT64")] ulong alignment);

        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetOffset@Allocation@D3D12MA@@QEBA_KXZ", ExactSpelling = true)]
        [return: NativeTypeName("UINT64")]
        public static extern ulong GetOffset(Allocation* pThis);

        [return: NativeTypeName("UINT64")]
        public ulong GetAlignment()
        {
            return m_Alignment;
        }

        [return: NativeTypeName("UINT64")]
        public ulong GetSize()
        {
            return m_Size;
        }

        public void* GetResource()
        {
            return m_Resource;
        }

        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetResource@Allocation@D3D12MA@@QEAAXPEAUID3D12Resource@@@Z", ExactSpelling = true)]
        public static extern void SetResource(Allocation* pThis, void* pResource);

        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetHeap@Allocation@D3D12MA@@QEBAPEAUID3D12Heap@@XZ", ExactSpelling = true)]
        public static extern void* GetHeap(Allocation* pThis);

        public void SetPrivateData(void* pPrivateData)
        {
            m_pPrivateData = pPrivateData;
        }

        public void* GetPrivateData()
        {
            return m_pPrivateData;
        }

        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetName@Allocation@D3D12MA@@QEAAXPEB_W@Z", ExactSpelling = true)]
        public static extern void SetName(Allocation* pThis, [NativeTypeName("LPCWSTR")] ushort* Name);

        [return: NativeTypeName("LPCWSTR")]
        public ushort* GetName()
        {
            return m_Name;
        }

        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?InitCommitted@Allocation@D3D12MA@@AEAAXPEAVCommittedAllocationList@2@@Z", ExactSpelling = true)]
        private static extern void InitCommitted(Allocation* pThis, [NativeTypeName("D3D12MA::CommittedAllocationList *")] CommittedAllocationList* list);

        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?InitPlaced@Allocation@D3D12MA@@AEAAX_KPEAVNormalBlock@2@@Z", ExactSpelling = true)]
        private static extern void InitPlaced(Allocation* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, [NativeTypeName("D3D12MA::NormalBlock *")] NormalBlock* block);

        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?InitHeap@Allocation@D3D12MA@@AEAAXPEAVCommittedAllocationList@2@PEAUID3D12Heap@@@Z", ExactSpelling = true)]
        private static extern void InitHeap(Allocation* pThis, [NativeTypeName("D3D12MA::CommittedAllocationList *")] CommittedAllocationList* list, ID3D12Heap* heap);

        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SwapBlockAllocation@Allocation@D3D12MA@@AEAAXPEAV12@@Z", ExactSpelling = true)]
        private static extern void SwapBlockAllocation(Allocation* pThis, [NativeTypeName("D3D12MA::Allocation *")] Allocation* allocation);

        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetAllocHandle@Allocation@D3D12MA@@AEBA_KXZ", ExactSpelling = true)]
        [return: NativeTypeName("D3D12MA::AllocHandle")]
        private static extern ulong GetAllocHandle(Allocation* pThis);

        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetBlock@Allocation@D3D12MA@@AEAAPEAVNormalBlock@2@XZ", ExactSpelling = true)]
        [return: NativeTypeName("D3D12MA::NormalBlock *")]
        private static extern NormalBlock* GetBlock(Allocation* pThis);

        [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?FreeName@Allocation@D3D12MA@@AEAAXXZ", ExactSpelling = true)]
        private static extern void FreeName(Allocation* pThis);

        private enum Type
        {
            TYPE_COMMITTED,
            TYPE_PLACED,
            TYPE_HEAP,
            TYPE_COUNT,
        }

        [StructLayout(LayoutKind.Explicit)]
        private unsafe partial struct _Anonymous_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_D3D12MemAlloc_L623_C9")]
            public _m_Committed_e__Struct m_Committed;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_D3D12MemAlloc_L630_C9")]
            public _m_Placed_e__Struct m_Placed;

            [FieldOffset(0)]
            [NativeTypeName("__AnonymousRecord_D3D12MemAlloc_L636_C9")]
            public _m_Heap_e__Struct m_Heap;

            public unsafe partial struct _m_Committed_e__Struct
            {
                [NativeTypeName("D3D12MA::CommittedAllocationList *")]
                public CommittedAllocationList* list;

                [NativeTypeName("D3D12MA::Allocation *")]
                public Allocation* prev;

                [NativeTypeName("D3D12MA::Allocation *")]
                public Allocation* next;
            }

            public unsafe partial struct _m_Placed_e__Struct
            {
                [NativeTypeName("D3D12MA::AllocHandle")]
                public ulong allocHandle;

                [NativeTypeName("D3D12MA::NormalBlock *")]
                public NormalBlock* block;
            }

            public unsafe partial struct _m_Heap_e__Struct
            {
                [NativeTypeName("D3D12MA::CommittedAllocationList *")]
                public CommittedAllocationList* list;

                [NativeTypeName("D3D12MA::Allocation *")]
                public Allocation* prev;

                [NativeTypeName("D3D12MA::Allocation *")]
                public Allocation* next;

                public void* heap;
            }
        }

        private partial struct PackedData
        {
            public uint _bitfield1;

            [NativeTypeName("uint : 2")]
            private uint m_Type
            {
                readonly get
                {
                    return _bitfield1 & 0x3u;
                }

                set
                {
                    _bitfield1 = (_bitfield1 & ~0x3u) | (value & 0x3u);
                }
            }

            [NativeTypeName("uint : 3")]
            private uint m_ResourceDimension
            {
                readonly get
                {
                    return (_bitfield1 >> 2) & 0x7u;
                }

                set
                {
                    _bitfield1 = (_bitfield1 & ~(0x7u << 2)) | ((value & 0x7u) << 2);
                }
            }

            [NativeTypeName("uint : 24")]
            private uint m_ResourceFlags
            {
                readonly get
                {
                    return (_bitfield1 >> 5) & 0xFFFFFFu;
                }

                set
                {
                    _bitfield1 = (_bitfield1 & ~(0xFFFFFFu << 5)) | ((value & 0xFFFFFFu) << 5);
                }
            }

            public uint _bitfield2;

            [NativeTypeName("uint : 9")]
            private uint m_TextureLayout
            {
                readonly get
                {
                    return _bitfield2 & 0x1FFu;
                }

                set
                {
                    _bitfield2 = (_bitfield2 & ~0x1FFu) | (value & 0x1FFu);
                }
            }

            public PackedData()
            {
                m_Type = 0;
                m_ResourceDimension = 0;
                m_ResourceFlags = 0;
                m_TextureLayout = 0;
            }

            [return: NativeTypeName("D3D12MA::Allocation::Type")]
            public new Type GetType()
            {
                return (Type)(m_Type);
            }

            public ResourceDimension GetResourceDimension()
            {
                return (ResourceDimension)(m_ResourceDimension);
            }

            public ResourceFlags GetResourceFlags()
            {
                return (ResourceFlags)(m_ResourceFlags);
            }

            public TextureLayout GetTextureLayout()
            {
                return (TextureLayout)(m_TextureLayout);
            }

            [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetType@PackedData@Allocation@D3D12MA@@QEAAXW4Type@23@@Z", ExactSpelling = true)]
            public static extern void SetType(PackedData* pThis, [NativeTypeName("D3D12MA::Allocation::Type")] Type type);

            [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetResourceDimension@PackedData@Allocation@D3D12MA@@QEAAXW4D3D12_RESOURCE_DIMENSION@@@Z", ExactSpelling = true)]
            public static extern void SetResourceDimension(PackedData* pThis, ResourceDimension resourceDimension);

            [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetResourceFlags@PackedData@Allocation@D3D12MA@@QEAAXW4D3D12_RESOURCE_FLAGS@@@Z", ExactSpelling = true)]
            public static extern void SetResourceFlags(PackedData* pThis, ResourceFlags resourceFlags);

            [DllImport("d3d12ma.dll", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetTextureLayout@PackedData@Allocation@D3D12MA@@QEAAXW4D3D12_TEXTURE_LAYOUT@@@Z", ExactSpelling = true)]
            public static extern void SetTextureLayout(PackedData* pThis, TextureLayout textureLayout);
        }
    }
}
