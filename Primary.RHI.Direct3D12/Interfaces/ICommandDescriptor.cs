using Primary.RHI.Direct3D12.Descriptors;
using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12.Interfaces
{
    internal interface ICommandDescriptor
    {
        public CpuDescriptorHandle CpuDescriptor { get; }
        public ResourceType BindType { get; }
        public bool IsDynamic { get; }

        public void AllocateDynamic(uint offset, CpuDescriptorHandle handle);
    }
}
