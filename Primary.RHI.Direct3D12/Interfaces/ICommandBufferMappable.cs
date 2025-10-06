using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12.Interfaces
{
    internal interface ICommandBufferMappable : ICommandBufferResource
    {
        public ulong TotalSizeInBytes { get; }

        public void MappableCopyDataTo(ID3D12GraphicsCommandList7 commandList, ref SimpleMapInfo allocation);
    }
}
