namespace Interop.D3D12MemAlloc
{
    public partial struct Budget
    {
        [NativeTypeName("D3D12MA::Statistics")]
        public Statistics Stats;

        [NativeTypeName("UINT64")]
        public ulong UsageBytes;

        [NativeTypeName("UINT64")]
        public ulong BudgetBytes;
    }
}
