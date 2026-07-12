using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using static Interop.D3D12MemAlloc.ALLOCATION_FLAGS;
using static Interop.D3D12MemAlloc.POOL_FLAGS;
using static Interop.D3D12MemAlloc.VIRTUAL_ALLOCATION_FLAGS;
using static Interop.D3D12MemAlloc.VIRTUAL_BLOCK_FLAGS;

using DXGI_FORMAT = Silk.NET.DXGI.Format;
using D3D12_TEXTURE_LAYOUT = Silk.NET.Direct3D12.TextureLayout;
using D3D12_RESOURCE_STATES = Silk.NET.Direct3D12.ResourceStates;
using D3D12_RESOURCE_FLAGS = Silk.NET.Direct3D12.ResourceFlags;
using D3D12_RESOURCE_DIMENSION = Silk.NET.Direct3D12.ResourceDimension;
using D3D12_RESIDENCY_PRIORITY = Silk.NET.Direct3D12.ResidencyPriority;
using D3D12_HEAP_TYPE = Silk.NET.Direct3D12.HeapType;
using D3D12_HEAP_FLAGS = Silk.NET.Direct3D12.HeapFlags;
using D3D12_BARRIER_LAYOUT = Silk.NET.Direct3D12.BarrierLayout;

using D3D12_RESOURCE_DESC1 = Silk.NET.Direct3D12.ResourceDesc1;
using D3D12_RESOURCE_DESC = Silk.NET.Direct3D12.ResourceDesc;
using D3D12_RESOURCE_ALLOCATION_INFO = Silk.NET.Direct3D12.ResourceAllocationInfo;
using D3D12_HEAP_PROPERTIES = Silk.NET.Direct3D12.HeapProperties;
using D3D12_FEATURE_DATA_D3D12_OPTIONS = Silk.NET.Direct3D12.FeatureDataD3D12Options;
using D3D12_CLEAR_VALUE = Silk.NET.Direct3D12.ClearValue;

namespace Interop.D3D12MemAlloc
{
    [NativeTypeName("struct IUnknownImpl : IUnknown")]
    public unsafe partial struct IUnknownImpl
    {
        public void** lpVtbl;

        [NativeTypeName("atomic<UINT>")]
        private volatile uint m_RefCount;

        [VtblIndex(0)]
        [return: NativeTypeName("HRESULT")]
        public int QueryInterface([NativeTypeName("const IID &")] Guid* riid, void** ppvObject)
        {
            return ((delegate* unmanaged[Stdcall]<IUnknownImpl*, Guid*, void**, int>)(lpVtbl[0]))((IUnknownImpl*)Unsafe.AsPointer(ref this), riid, ppvObject);
        }

        [VtblIndex(1)]
        [return: NativeTypeName("ULONG")]
        public uint AddRef()
        {
            return ((delegate* unmanaged[Stdcall]<IUnknownImpl*, uint>)(lpVtbl[1]))((IUnknownImpl*)Unsafe.AsPointer(ref this));
        }

        [VtblIndex(2)]
        [return: NativeTypeName("ULONG")]
        public uint Release()
        {
            return ((delegate* unmanaged[Stdcall]<IUnknownImpl*, uint>)(lpVtbl[2]))((IUnknownImpl*)Unsafe.AsPointer(ref this));
        }

        [VtblIndex(3)]
        public void Dispose()
        {
            ((delegate* unmanaged[Thiscall]<IUnknownImpl*, void>)(lpVtbl[3]))((IUnknownImpl*)Unsafe.AsPointer(ref this));
        }

        [VtblIndex(4)]
        public void ReleaseThis()
        {
            ((delegate* unmanaged[Thiscall]<IUnknownImpl*, void>)(lpVtbl[4]))((IUnknownImpl*)Unsafe.AsPointer(ref this));
        }
    }

    public partial struct DefragmentationContextPimpl
    {
    }

    public partial struct AllocatorPimpl
    {
    }

    public partial struct PoolPimpl
    {
    }

    public partial struct NormalBlock
    {
    }

    public partial struct BlockVector
    {
    }

    public partial struct CommittedAllocationList
    {
    }

    public partial struct JsonWriter
    {
    }

    public partial struct VirtualBlockPimpl
    {
    }

    public unsafe partial struct ALLOCATION_CALLBACKS
    {
        [NativeTypeName("D3D12MA::ALLOCATE_FUNC_PTR")]
        public delegate* unmanaged[Cdecl]<nuint, nuint, void*, void*> pAllocate;

        [NativeTypeName("D3D12MA::FREE_FUNC_PTR")]
        public delegate* unmanaged[Cdecl]<void*, void*, void> pFree;

        public void* pPrivateData;
    }

    public enum ALLOCATION_FLAGS
    {
        ALLOCATION_FLAG_NONE = 0,
        ALLOCATION_FLAG_COMMITTED = 0x1,
        ALLOCATION_FLAG_NEVER_ALLOCATE = 0x2,
        ALLOCATION_FLAG_WITHIN_BUDGET = 0x4,
        ALLOCATION_FLAG_UPPER_ADDRESS = 0x8,
        ALLOCATION_FLAG_CAN_ALIAS = 0x10,
        ALLOCATION_FLAG_STRATEGY_MIN_MEMORY = 0x00010000,
        ALLOCATION_FLAG_STRATEGY_MIN_TIME = 0x00020000,
        ALLOCATION_FLAG_STRATEGY_MIN_OFFSET = 0x0004000,
        ALLOCATION_FLAG_STRATEGY_BEST_FIT = ALLOCATION_FLAG_STRATEGY_MIN_MEMORY,
        ALLOCATION_FLAG_STRATEGY_FIRST_FIT = ALLOCATION_FLAG_STRATEGY_MIN_TIME,
        ALLOCATION_FLAG_STRATEGY_MASK = ALLOCATION_FLAG_STRATEGY_MIN_MEMORY | ALLOCATION_FLAG_STRATEGY_MIN_TIME | ALLOCATION_FLAG_STRATEGY_MIN_OFFSET,
    }

    public unsafe partial struct ALLOCATION_DESC
    {
        [NativeTypeName("D3D12MA::ALLOCATION_FLAGS")]
        public ALLOCATION_FLAGS Flags;

        public D3D12_HEAP_TYPE HeapType;

        public D3D12_HEAP_FLAGS ExtraHeapFlags;

        [NativeTypeName("D3D12MA::Pool *")]
        public Pool* CustomPool;

        public void* pPrivateData;
    }

    public partial struct Statistics
    {
        public uint BlockCount;

        public uint AllocationCount;

        [NativeTypeName("UINT64")]
        public ulong BlockBytes;

        [NativeTypeName("UINT64")]
        public ulong AllocationBytes;
    }

    public partial struct DetailedStatistics
    {
        [NativeTypeName("D3D12MA::Statistics")]
        public Statistics Stats;

        public uint UnusedRangeCount;

        [NativeTypeName("UINT64")]
        public ulong AllocationSizeMin;

        [NativeTypeName("UINT64")]
        public ulong AllocationSizeMax;

        [NativeTypeName("UINT64")]
        public ulong UnusedRangeSizeMin;

        [NativeTypeName("UINT64")]
        public ulong UnusedRangeSizeMax;
    }

    public partial struct TotalStatistics
    {
        [NativeTypeName("DetailedStatistics[5]")]
        public _HeapType_e__FixedBuffer HeapType;

        [NativeTypeName("DetailedStatistics[2]")]
        public _MemorySegmentGroup_e__FixedBuffer MemorySegmentGroup;

        [NativeTypeName("D3D12MA::DetailedStatistics")]
        public DetailedStatistics Total;

        [InlineArray(5)]
        public partial struct _HeapType_e__FixedBuffer
        {
            public DetailedStatistics e0;
        }

        [InlineArray(2)]
        public partial struct _MemorySegmentGroup_e__FixedBuffer
        {
            public DetailedStatistics e0;
        }
    }

    public partial struct Budget
    {
        [NativeTypeName("D3D12MA::Statistics")]
        public Statistics Stats;

        [NativeTypeName("UINT64")]
        public ulong UsageBytes;

        [NativeTypeName("UINT64")]
        public ulong BudgetBytes;
    }

    public partial struct VirtualAllocation
    {
        [NativeTypeName("D3D12MA::AllocHandle")]
        public ulong AllocHandle;
    }

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

        private ID3D12Resource* m_Resource;

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

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetOffset@Allocation@D3D12MA@@QEBA_KXZ", ExactSpelling = true)]
        [return: NativeTypeName("UINT64")]
        public static extern ulong GetOffset(Allocation* pThis);

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetAlignment()
        {
            return m_Alignment;
        }

        [return: NativeTypeName("UINT64")]
        public readonly ulong GetSize()
        {
            return m_Size;
        }

        public readonly ID3D12Resource* GetResource()
        {
            return m_Resource;
        }

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetResource@Allocation@D3D12MA@@QEAAXPEAUID3D12Resource@@@Z", ExactSpelling = true)]
        public static extern void SetResource(Allocation* pThis, ID3D12Resource* pResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetHeap@Allocation@D3D12MA@@QEBAPEAUID3D12Heap@@XZ", ExactSpelling = true)]
        public static extern ID3D12Heap* GetHeap(Allocation* pThis);

        public void SetPrivateData(void* pPrivateData)
        {
            m_pPrivateData = pPrivateData;
        }

        public readonly void* GetPrivateData()
        {
            return m_pPrivateData;
        }

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetName@Allocation@D3D12MA@@QEAAXPEB_W@Z", ExactSpelling = true)]
        public static extern void SetName(Allocation* pThis, [NativeTypeName("LPCWSTR")] ushort* Name);

        [return: NativeTypeName("LPCWSTR")]
        public readonly ushort* GetName()
        {
            return m_Name;
        }

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?InitCommitted@Allocation@D3D12MA@@AEAAXPEAVCommittedAllocationList@2@@Z", ExactSpelling = true)]
        private static extern void InitCommitted(Allocation* pThis, [NativeTypeName("D3D12MA::CommittedAllocationList *")] CommittedAllocationList* list);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?InitPlaced@Allocation@D3D12MA@@AEAAX_KPEAVNormalBlock@2@@Z", ExactSpelling = true)]
        private static extern void InitPlaced(Allocation* pThis, [NativeTypeName("D3D12MA::AllocHandle")] ulong allocHandle, [NativeTypeName("D3D12MA::NormalBlock *")] NormalBlock* block);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?InitHeap@Allocation@D3D12MA@@AEAAXPEAVCommittedAllocationList@2@PEAUID3D12Heap@@@Z", ExactSpelling = true)]
        private static extern void InitHeap(Allocation* pThis, [NativeTypeName("D3D12MA::CommittedAllocationList *")] CommittedAllocationList* list, ID3D12Heap* heap);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SwapBlockAllocation@Allocation@D3D12MA@@AEAAXPEAV12@@Z", ExactSpelling = true)]
        private static extern void SwapBlockAllocation(Allocation* pThis, [NativeTypeName("D3D12MA::Allocation *")] Allocation* allocation);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetAllocHandle@Allocation@D3D12MA@@AEBA_KXZ", ExactSpelling = true)]
        [return: NativeTypeName("D3D12MA::AllocHandle")]
        private static extern ulong GetAllocHandle(Allocation* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetBlock@Allocation@D3D12MA@@AEAAPEAVNormalBlock@2@XZ", ExactSpelling = true)]
        [return: NativeTypeName("D3D12MA::NormalBlock *")]
        private static extern NormalBlock* GetBlock(Allocation* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?FreeName@Allocation@D3D12MA@@AEAAXXZ", ExactSpelling = true)]
        private static extern void FreeName(Allocation* pThis);

        private enum Type
        {
            TYPE_COMMITTED,
            TYPE_PLACED,
            TYPE_HEAP,
            TYPE_COUNT,
        }

        [StructLayout(LayoutKind.Explicit)]
        private partial struct _Anonymous_e__Union
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

                public ID3D12Heap* heap;
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
            public new readonly Type GetType()
            {
                return (Type)(m_Type);
            }

            public readonly D3D12_RESOURCE_DIMENSION GetResourceDimension()
            {
                return (D3D12_RESOURCE_DIMENSION)(m_ResourceDimension);
            }

            public readonly D3D12_RESOURCE_FLAGS GetResourceFlags()
            {
                return (D3D12_RESOURCE_FLAGS)(m_ResourceFlags);
            }

            public readonly D3D12_TEXTURE_LAYOUT GetTextureLayout()
            {
                return (D3D12_TEXTURE_LAYOUT)(m_TextureLayout);
            }

            [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetType@PackedData@Allocation@D3D12MA@@QEAAXW4Type@23@@Z", ExactSpelling = true)]
            public static extern void SetType(PackedData* pThis, [NativeTypeName("D3D12MA::Allocation::Type")] Type type);

            [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetResourceDimension@PackedData@Allocation@D3D12MA@@QEAAXW4D3D12_RESOURCE_DIMENSION@@@Z", ExactSpelling = true)]
            public static extern void SetResourceDimension(PackedData* pThis, D3D12_RESOURCE_DIMENSION resourceDimension);

            [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetResourceFlags@PackedData@Allocation@D3D12MA@@QEAAXW4D3D12_RESOURCE_FLAGS@@@Z", ExactSpelling = true)]
            public static extern void SetResourceFlags(PackedData* pThis, D3D12_RESOURCE_FLAGS resourceFlags);

            [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetTextureLayout@PackedData@Allocation@D3D12MA@@QEAAXW4D3D12_TEXTURE_LAYOUT@@@Z", ExactSpelling = true)]
            public static extern void SetTextureLayout(PackedData* pThis, D3D12_TEXTURE_LAYOUT textureLayout);
        }
    }

    public enum DEFRAGMENTATION_FLAGS
    {
        DEFRAGMENTATION_FLAG_ALGORITHM_FAST = 0x1,
        DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED = 0x2,
        DEFRAGMENTATION_FLAG_ALGORITHM_FULL = 0x4,
        DEFRAGMENTATION_FLAG_ALGORITHM_MASK = DEFRAGMENTATION_FLAG_ALGORITHM_FAST | DEFRAGMENTATION_FLAG_ALGORITHM_BALANCED | DEFRAGMENTATION_FLAG_ALGORITHM_FULL,
    }

    public partial struct DEFRAGMENTATION_DESC
    {
        [NativeTypeName("D3D12MA::DEFRAGMENTATION_FLAGS")]
        public DEFRAGMENTATION_FLAGS Flags;

        [NativeTypeName("UINT64")]
        public ulong MaxBytesPerPass;

        [NativeTypeName("UINT32")]
        public uint MaxAllocationsPerPass;
    }

    public enum DEFRAGMENTATION_MOVE_OPERATION
    {
        DEFRAGMENTATION_MOVE_OPERATION_COPY = 0,
        DEFRAGMENTATION_MOVE_OPERATION_IGNORE = 1,
        DEFRAGMENTATION_MOVE_OPERATION_DESTROY = 2,
    }

    public unsafe partial struct DEFRAGMENTATION_MOVE
    {
        [NativeTypeName("D3D12MA::DEFRAGMENTATION_MOVE_OPERATION")]
        public DEFRAGMENTATION_MOVE_OPERATION Operation;

        [NativeTypeName("D3D12MA::Allocation *")]
        public Allocation* pSrcAllocation;

        [NativeTypeName("D3D12MA::Allocation *")]
        public Allocation* pDstTmpAllocation;
    }

    public unsafe partial struct DEFRAGMENTATION_PASS_MOVE_INFO
    {
        [NativeTypeName("UINT32")]
        public uint MoveCount;

        [NativeTypeName("D3D12MA::DEFRAGMENTATION_MOVE *")]
        public DEFRAGMENTATION_MOVE* pMoves;
    }

    public partial struct DEFRAGMENTATION_STATS
    {
        [NativeTypeName("UINT64")]
        public ulong BytesMoved;

        [NativeTypeName("UINT64")]
        public ulong BytesFreed;

        [NativeTypeName("UINT32")]
        public uint AllocationsMoved;

        [NativeTypeName("UINT32")]
        public uint HeapsFreed;
    }

    [NativeTypeName("struct DefragmentationContext : D3D12MA::IUnknownImpl")]
    public unsafe partial struct DefragmentationContext
    {
        public IUnknownImpl Base;

        [NativeTypeName("D3D12MA::DefragmentationContextPimpl *")]
        private DefragmentationContextPimpl* m_Pimpl;

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?BeginPass@DefragmentationContext@D3D12MA@@QEAAJPEAUDEFRAGMENTATION_PASS_MOVE_INFO@2@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int BeginPass(DefragmentationContext* pThis, [NativeTypeName("D3D12MA::DEFRAGMENTATION_PASS_MOVE_INFO *")] DEFRAGMENTATION_PASS_MOVE_INFO* pPassInfo);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?EndPass@DefragmentationContext@D3D12MA@@QEAAJPEAUDEFRAGMENTATION_PASS_MOVE_INFO@2@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int EndPass(DefragmentationContext* pThis, [NativeTypeName("D3D12MA::DEFRAGMENTATION_PASS_MOVE_INFO *")] DEFRAGMENTATION_PASS_MOVE_INFO* pPassInfo);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetStats@DefragmentationContext@D3D12MA@@QEAAXPEAUDEFRAGMENTATION_STATS@2@@Z", ExactSpelling = true)]
        public static extern void GetStats(DefragmentationContext* pThis, [NativeTypeName("D3D12MA::DEFRAGMENTATION_STATS *")] DEFRAGMENTATION_STATS* pStats);
    }

    public enum POOL_FLAGS
    {
        POOL_FLAG_NONE = 0,
        POOL_FLAG_ALGORITHM_LINEAR = 0x1,
        POOL_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED = 0x2,
        POOL_FLAG_ALWAYS_COMMITTED = 0x4,
        POOL_FLAG_DONT_USE_TIGHT_ALIGNMENT = 0x8,
        POOL_FLAG_ALGORITHM_MASK = POOL_FLAG_ALGORITHM_LINEAR,
    }

    public unsafe partial struct POOL_DESC
    {
        [NativeTypeName("D3D12MA::POOL_FLAGS")]
        public POOL_FLAGS Flags;

        public D3D12_HEAP_PROPERTIES HeapProperties;

        public D3D12_HEAP_FLAGS HeapFlags;

        [NativeTypeName("UINT64")]
        public ulong BlockSize;

        public uint MinBlockCount;

        public uint MaxBlockCount;

        [NativeTypeName("UINT64")]
        public ulong MinAllocationAlignment;

        public ID3D12ProtectedResourceSession* pProtectedSession;

        public D3D12_RESIDENCY_PRIORITY ResidencyPriority;
    }

    [NativeTypeName("struct Pool : D3D12MA::IUnknownImpl")]
    public unsafe partial struct Pool
    {
        public IUnknownImpl Base;

        [NativeTypeName("D3D12MA::PoolPimpl *")]
        private PoolPimpl* m_Pimpl;

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetDesc@Pool@D3D12MA@@QEBA?AUPOOL_DESC@2@XZ", ExactSpelling = true)]
        [return: NativeTypeName("D3D12MA::POOL_DESC")]
        public static extern POOL_DESC GetDesc(Pool* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetStatistics@Pool@D3D12MA@@QEAAXPEAUStatistics@2@@Z", ExactSpelling = true)]
        public static extern void GetStatistics(Pool* pThis, [NativeTypeName("D3D12MA::Statistics *")] Statistics* pStats);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CalculateStatistics@Pool@D3D12MA@@QEAAXPEAUDetailedStatistics@2@@Z", ExactSpelling = true)]
        public static extern void CalculateStatistics(Pool* pThis, [NativeTypeName("D3D12MA::DetailedStatistics *")] DetailedStatistics* pStats);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetName@Pool@D3D12MA@@QEAAXPEB_W@Z", ExactSpelling = true)]
        public static extern void SetName(Pool* pThis, [NativeTypeName("LPCWSTR")] ushort* Name);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetName@Pool@D3D12MA@@QEBAPEB_WXZ", ExactSpelling = true)]
        [return: NativeTypeName("LPCWSTR")]
        public static extern ushort* GetName(Pool* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?BeginDefragmentation@Pool@D3D12MA@@QEAAJPEBUDEFRAGMENTATION_DESC@2@PEAPEAVDefragmentationContext@2@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int BeginDefragmentation(Pool* pThis, [NativeTypeName("const DEFRAGMENTATION_DESC *")] DEFRAGMENTATION_DESC* pDesc, DefragmentationContext** ppContext);
    }

    public enum ALLOCATOR_FLAGS
    {
        ALLOCATOR_FLAG_NONE = 0,
        ALLOCATOR_FLAG_SINGLETHREADED = 0x1,
        ALLOCATOR_FLAG_ALWAYS_COMMITTED = 0x2,
        ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED = 0x4,
        ALLOCATOR_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED = 0x8,
        ALLOCATOR_FLAG_DONT_PREFER_SMALL_BUFFERS_COMMITTED = 0x10,
        ALLOCATOR_FLAG_DONT_USE_TIGHT_ALIGNMENT = 0x20,
    }

    public unsafe partial struct ALLOCATOR_DESC
    {
        [NativeTypeName("D3D12MA::ALLOCATOR_FLAGS")]
        public ALLOCATOR_FLAGS Flags;

        public ID3D12Device* pDevice;

        [NativeTypeName("UINT64")]
        public ulong PreferredBlockSize;

        [NativeTypeName("const ALLOCATION_CALLBACKS *")]
        public ALLOCATION_CALLBACKS* pAllocationCallbacks;

        public IDXGIAdapter* pAdapter;
    }

    [NativeTypeName("struct Allocator : D3D12MA::IUnknownImpl")]
    public unsafe partial struct Allocator
    {
        public IUnknownImpl Base;

        [NativeTypeName("D3D12MA::AllocatorPimpl *")]
        private AllocatorPimpl* m_Pimpl;

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetD3D12Options@Allocator@D3D12MA@@QEBAAEBUD3D12_FEATURE_DATA_D3D12_OPTIONS@@XZ", ExactSpelling = true)]
        [return: NativeTypeName("const D3D12_FEATURE_DATA_D3D12_OPTIONS &")]
        public static extern D3D12_FEATURE_DATA_D3D12_OPTIONS* GetD3D12Options(Allocator* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?IsUMA@Allocator@D3D12MA@@QEBAHXZ", ExactSpelling = true)]
        [return: NativeTypeName("BOOL")]
        public static extern int IsUMA(Allocator* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?IsCacheCoherentUMA@Allocator@D3D12MA@@QEBAHXZ", ExactSpelling = true)]
        [return: NativeTypeName("BOOL")]
        public static extern int IsCacheCoherentUMA(Allocator* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?IsGPUUploadHeapSupported@Allocator@D3D12MA@@QEBAHXZ", ExactSpelling = true)]
        [return: NativeTypeName("BOOL")]
        public static extern int IsGPUUploadHeapSupported(Allocator* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?IsTightAlignmentSupported@Allocator@D3D12MA@@QEBAHXZ", ExactSpelling = true)]
        [return: NativeTypeName("BOOL")]
        public static extern int IsTightAlignmentSupported(Allocator* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetMemoryCapacity@Allocator@D3D12MA@@QEBA_KI@Z", ExactSpelling = true)]
        [return: NativeTypeName("UINT64")]
        public static extern ulong GetMemoryCapacity(Allocator* pThis, uint memorySegmentGroup);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreateResource@Allocator@D3D12MA@@QEAAJPEBUALLOCATION_DESC@2@PEBUD3D12_RESOURCE_DESC@@W4D3D12_RESOURCE_STATES@@PEBUD3D12_CLEAR_VALUE@@PEAPEAVAllocation@2@AEBU_GUID@@PEAPEAX@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateResource(Allocator* pThis, [NativeTypeName("const ALLOCATION_DESC *")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC *")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, Allocation** ppAllocation, [NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreateResource2@Allocator@D3D12MA@@QEAAJPEBUALLOCATION_DESC@2@PEBUD3D12_RESOURCE_DESC1@@W4D3D12_RESOURCE_STATES@@PEBUD3D12_CLEAR_VALUE@@PEAPEAVAllocation@2@AEBU_GUID@@PEAPEAX@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateResource2(Allocator* pThis, [NativeTypeName("const ALLOCATION_DESC *")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, Allocation** ppAllocation, [NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreateResource3@Allocator@D3D12MA@@QEAAJPEBUALLOCATION_DESC@2@PEBUD3D12_RESOURCE_DESC1@@W4D3D12_BARRIER_LAYOUT@@PEBUD3D12_CLEAR_VALUE@@IPEBW4DXGI_FORMAT@@PEAPEAVAllocation@2@AEBU_GUID@@PEAPEAX@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateResource3(Allocator* pThis, [NativeTypeName("const ALLOCATION_DESC *")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, Allocation** ppAllocation, [NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?AllocateMemory@Allocator@D3D12MA@@QEAAJPEBUALLOCATION_DESC@2@PEBUD3D12_RESOURCE_ALLOCATION_INFO@@PEAPEAVAllocation@2@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int AllocateMemory(Allocator* pThis, [NativeTypeName("const ALLOCATION_DESC *")] ALLOCATION_DESC* pAllocDesc, [NativeTypeName("const D3D12_RESOURCE_ALLOCATION_INFO *")] D3D12_RESOURCE_ALLOCATION_INFO* pAllocInfo, Allocation** ppAllocation);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreateAliasingResource@Allocator@D3D12MA@@QEAAJPEAVAllocation@2@_KPEBUD3D12_RESOURCE_DESC@@W4D3D12_RESOURCE_STATES@@PEBUD3D12_CLEAR_VALUE@@AEBU_GUID@@PEAPEAX@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateAliasingResource(Allocator* pThis, [NativeTypeName("D3D12MA::Allocation *")] Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC *")] D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreateAliasingResource1@Allocator@D3D12MA@@QEAAJPEAVAllocation@2@_KPEBUD3D12_RESOURCE_DESC1@@W4D3D12_RESOURCE_STATES@@PEBUD3D12_CLEAR_VALUE@@AEBU_GUID@@PEAPEAX@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateAliasingResource1(Allocator* pThis, [NativeTypeName("D3D12MA::Allocation *")] Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_RESOURCE_STATES InitialResourceState, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreateAliasingResource2@Allocator@D3D12MA@@QEAAJPEAVAllocation@2@_KPEBUD3D12_RESOURCE_DESC1@@W4D3D12_BARRIER_LAYOUT@@PEBUD3D12_CLEAR_VALUE@@IPEBW4DXGI_FORMAT@@AEBU_GUID@@PEAPEAX@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreateAliasingResource2(Allocator* pThis, [NativeTypeName("D3D12MA::Allocation *")] Allocation* pAllocation, [NativeTypeName("UINT64")] ulong AllocationLocalOffset, [NativeTypeName("const D3D12_RESOURCE_DESC1 *")] D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT InitialLayout, [NativeTypeName("const D3D12_CLEAR_VALUE *")] D3D12_CLEAR_VALUE* pOptimizedClearValue, [NativeTypeName("UINT32")] uint NumCastableFormats, [NativeTypeName("const DXGI_FORMAT *")] DXGI_FORMAT* pCastableFormats, [NativeTypeName("const IID &")] Guid* riidResource, void** ppvResource);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CreatePool@Allocator@D3D12MA@@QEAAJPEBUPOOL_DESC@2@PEAPEAVPool@2@@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int CreatePool(Allocator* pThis, [NativeTypeName("const POOL_DESC *")] POOL_DESC* pPoolDesc, Pool** ppPool);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetCurrentFrameIndex@Allocator@D3D12MA@@QEAAXI@Z", ExactSpelling = true)]
        public static extern void SetCurrentFrameIndex(Allocator* pThis, uint frameIndex);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetBudget@Allocator@D3D12MA@@QEAAXPEAUBudget@2@0@Z", ExactSpelling = true)]
        public static extern void GetBudget(Allocator* pThis, [NativeTypeName("D3D12MA::Budget *")] Budget* pLocalBudget, [NativeTypeName("D3D12MA::Budget *")] Budget* pNonLocalBudget);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CalculateStatistics@Allocator@D3D12MA@@QEAAXPEAUTotalStatistics@2@@Z", ExactSpelling = true)]
        public static extern void CalculateStatistics(Allocator* pThis, [NativeTypeName("D3D12MA::TotalStatistics *")] TotalStatistics* pStats);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?BuildStatsString@Allocator@D3D12MA@@QEBAXPEAPEA_WH@Z", ExactSpelling = true)]
        public static extern void BuildStatsString(Allocator* pThis, [NativeTypeName("WCHAR **")] ushort** ppStatsString, [NativeTypeName("BOOL")] int DetailedMap);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?FreeStatsString@Allocator@D3D12MA@@QEBAXPEA_W@Z", ExactSpelling = true)]
        public static extern void FreeStatsString(Allocator* pThis, [NativeTypeName("WCHAR *")] ushort* pStatsString);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?BeginDefragmentation@Allocator@D3D12MA@@QEAAXPEBUDEFRAGMENTATION_DESC@2@PEAPEAVDefragmentationContext@2@@Z", ExactSpelling = true)]
        public static extern void BeginDefragmentation(Allocator* pThis, [NativeTypeName("const DEFRAGMENTATION_DESC *")] DEFRAGMENTATION_DESC* pDesc, DefragmentationContext** ppContext);
    }

    public enum VIRTUAL_BLOCK_FLAGS
    {
        VIRTUAL_BLOCK_FLAG_NONE = 0,
        VIRTUAL_BLOCK_FLAG_ALGORITHM_LINEAR = POOL_FLAG_ALGORITHM_LINEAR,
        VIRTUAL_BLOCK_FLAG_ALGORITHM_MASK = POOL_FLAG_ALGORITHM_MASK,
    }

    public unsafe partial struct VIRTUAL_BLOCK_DESC
    {
        [NativeTypeName("D3D12MA::VIRTUAL_BLOCK_FLAGS")]
        public VIRTUAL_BLOCK_FLAGS Flags;

        [NativeTypeName("UINT64")]
        public ulong Size;

        [NativeTypeName("const ALLOCATION_CALLBACKS *")]
        public ALLOCATION_CALLBACKS* pAllocationCallbacks;
    }

    public enum VIRTUAL_ALLOCATION_FLAGS
    {
        VIRTUAL_ALLOCATION_FLAG_NONE = 0,
        VIRTUAL_ALLOCATION_FLAG_UPPER_ADDRESS = ALLOCATION_FLAG_UPPER_ADDRESS,
        VIRTUAL_ALLOCATION_FLAG_STRATEGY_MIN_MEMORY = ALLOCATION_FLAG_STRATEGY_MIN_MEMORY,
        VIRTUAL_ALLOCATION_FLAG_STRATEGY_MIN_TIME = ALLOCATION_FLAG_STRATEGY_MIN_TIME,
        VIRTUAL_ALLOCATION_FLAG_STRATEGY_MIN_OFFSET = ALLOCATION_FLAG_STRATEGY_MIN_OFFSET,
        VIRTUAL_ALLOCATION_FLAG_STRATEGY_MASK = ALLOCATION_FLAG_STRATEGY_MASK,
    }

    public unsafe partial struct VIRTUAL_ALLOCATION_DESC
    {
        [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_FLAGS")]
        public VIRTUAL_ALLOCATION_FLAGS Flags;

        [NativeTypeName("UINT64")]
        public ulong Size;

        [NativeTypeName("UINT64")]
        public ulong Alignment;

        public void* pPrivateData;
    }

    public unsafe partial struct VIRTUAL_ALLOCATION_INFO
    {
        [NativeTypeName("UINT64")]
        public ulong Offset;

        [NativeTypeName("UINT64")]
        public ulong Size;

        public void* pPrivateData;
    }

    [NativeTypeName("struct VirtualBlock : D3D12MA::IUnknownImpl")]
    public unsafe partial struct VirtualBlock
    {
        public IUnknownImpl Base;

        [NativeTypeName("D3D12MA::VirtualBlockPimpl *")]
        private VirtualBlockPimpl* m_Pimpl;

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?IsEmpty@VirtualBlock@D3D12MA@@QEBAHXZ", ExactSpelling = true)]
        [return: NativeTypeName("BOOL")]
        public static extern int IsEmpty(VirtualBlock* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetAllocationInfo@VirtualBlock@D3D12MA@@QEBAXUVirtualAllocation@2@PEAUVIRTUAL_ALLOCATION_INFO@2@@Z", ExactSpelling = true)]
        public static extern void GetAllocationInfo(VirtualBlock* pThis, [NativeTypeName("D3D12MA::VirtualAllocation")] VirtualAllocation allocation, [NativeTypeName("D3D12MA::VIRTUAL_ALLOCATION_INFO *")] VIRTUAL_ALLOCATION_INFO* pInfo);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?Allocate@VirtualBlock@D3D12MA@@QEAAJPEBUVIRTUAL_ALLOCATION_DESC@2@PEAUVirtualAllocation@2@PEA_K@Z", ExactSpelling = true)]
        [return: NativeTypeName("HRESULT")]
        public static extern int Allocate(VirtualBlock* pThis, [NativeTypeName("const VIRTUAL_ALLOCATION_DESC *")] VIRTUAL_ALLOCATION_DESC* pDesc, [NativeTypeName("D3D12MA::VirtualAllocation *")] VirtualAllocation* pAllocation, [NativeTypeName("UINT64 *")] ulong* pOffset);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?FreeAllocation@VirtualBlock@D3D12MA@@QEAAXUVirtualAllocation@2@@Z", ExactSpelling = true)]
        public static extern void FreeAllocation(VirtualBlock* pThis, [NativeTypeName("D3D12MA::VirtualAllocation")] VirtualAllocation allocation);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?Clear@VirtualBlock@D3D12MA@@QEAAXXZ", ExactSpelling = true)]
        public static extern void Clear(VirtualBlock* pThis);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?SetAllocationPrivateData@VirtualBlock@D3D12MA@@QEAAXUVirtualAllocation@2@PEAX@Z", ExactSpelling = true)]
        public static extern void SetAllocationPrivateData(VirtualBlock* pThis, [NativeTypeName("D3D12MA::VirtualAllocation")] VirtualAllocation allocation, void* pPrivateData);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?GetStatistics@VirtualBlock@D3D12MA@@QEBAXPEAUStatistics@2@@Z", ExactSpelling = true)]
        public static extern void GetStatistics(VirtualBlock* pThis, [NativeTypeName("D3D12MA::Statistics *")] Statistics* pStats);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?CalculateStatistics@VirtualBlock@D3D12MA@@QEBAXPEAUDetailedStatistics@2@@Z", ExactSpelling = true)]
        public static extern void CalculateStatistics(VirtualBlock* pThis, [NativeTypeName("D3D12MA::DetailedStatistics *")] DetailedStatistics* pStats);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?BuildStatsString@VirtualBlock@D3D12MA@@QEBAXPEAPEA_W@Z", ExactSpelling = true)]
        public static extern void BuildStatsString(VirtualBlock* pThis, [NativeTypeName("WCHAR **")] ushort** ppStatsString);

        [DllImport("d3d12ma", CallingConvention = CallingConvention.ThisCall, EntryPoint = "?FreeStatsString@VirtualBlock@D3D12MA@@QEBAXPEA_W@Z", ExactSpelling = true)]
        public static extern void FreeStatsString(VirtualBlock* pThis, [NativeTypeName("WCHAR *")] ushort* pStatsString);
    }
}

namespace Interop.D3D12MemAlloc
{
    /// <summary>Defines the type of a member as it was used in the native signature.</summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
    [Conditional("DEBUG")]
    internal sealed partial class NativeTypeNameAttribute : Attribute
    {
        private readonly string _name;

        /// <summary>Initializes a new instance of the <see cref="NativeTypeNameAttribute" /> class.</summary>
        /// <param name="name">The name of the type that was used in the native signature.</param>
        public NativeTypeNameAttribute(string name)
        {
            _name = name;
        }

        /// <summary>Gets the name of the type that was used in the native signature.</summary>
        public string Name => _name;
    }
}

namespace Interop.D3D12MemAlloc
{
    /// <summary>Defines the annotation found in a native declaration.</summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
    [Conditional("DEBUG")]
    internal sealed partial class NativeAnnotationAttribute : Attribute
    {
        private readonly string _annotation;

        /// <summary>Initializes a new instance of the <see cref="NativeAnnotationAttribute" /> class.</summary>
        /// <param name="annotation">The annotation that was used in the native declaration.</param>
        public NativeAnnotationAttribute(string annotation)
        {
            _annotation = annotation;
        }

        /// <summary>Gets the annotation that was used in the native declaration.</summary>
        public string Annotation => _annotation;
    }
}

namespace Interop.D3D12MemAlloc
{
    /// <summary>Defines the vtbl index of a method as it was in the native signature.</summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    [Conditional("DEBUG")]
    internal sealed partial class VtblIndexAttribute : Attribute
    {
        private readonly uint _index;

        /// <summary>Initializes a new instance of the <see cref="VtblIndexAttribute" /> class.</summary>
        /// <param name="index">The vtbl index of a method as it was in the native signature.</param>
        public VtblIndexAttribute(uint index)
        {
            _index = index;
        }

        /// <summary>Gets the vtbl index of a method as it was in the native signature.</summary>
        public uint Index => _index;
    }
}

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
