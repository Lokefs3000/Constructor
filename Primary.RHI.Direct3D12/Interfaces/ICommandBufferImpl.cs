using Primary.RHI.Direct3D12.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12.Interfaces
{
    internal interface ICommandBufferImpl
    {
        public ID3D12Fence ExecutionCompleteFence { get; }
        public ulong FenceValue { get; }

        public void SubmitToQueueInternal(ID3D12Fence? fenceToWaitFor, ulong valueToWaitFor);
        public void ResetFrameData();
    }

    internal readonly record struct SimpleMapInfo(DynamicAllocation Allocation, ulong DestinationOffset);
    internal readonly record struct TextureMapInfo(DynamicAllocation Allocation, TextureLocation Location, uint Subresource, uint RowPitch);
}
