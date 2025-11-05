using System.Runtime.CompilerServices;

namespace Interop.D3D12MemAlloc
{
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
}
