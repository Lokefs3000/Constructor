using Arch.LowLevel;
using System.Runtime.CompilerServices;
using Vortice.Vulkan;

using Vk = Vortice.Vulkan.Vulkan;

namespace Primary.RHI.Vulkan.Utility
{
    internal unsafe sealed class PipelineBarrierManager : IDisposable
    {
        private UnsafeList<VkBufferMemoryBarrier2> _bufferBarriers;
        private UnsafeList<VkImageMemoryBarrier2> _imageBarriers;

        private bool _disposedValue;

        internal PipelineBarrierManager()
        {
            _bufferBarriers = new UnsafeList<VkBufferMemoryBarrier2>(8);
            _imageBarriers = new UnsafeList<VkImageMemoryBarrier2>(8);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _bufferBarriers.Dispose();
                _imageBarriers.Dispose();

                _disposedValue = true;
            }
        }

        ~PipelineBarrierManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void TransitionPending(VkCommandBuffer commandBuffer)
        {
            VkDependencyInfo dependencyInfo = new()
            {
                sType = VkStructureType.DependencyInfo,
                pNext = null,
                dependencyFlags = VkDependencyFlags.None,
                memoryBarrierCount = 0,
                pMemoryBarriers = null,
                bufferMemoryBarrierCount = (uint)_bufferBarriers.Count,
                pBufferMemoryBarriers = (VkBufferMemoryBarrier2*)Unsafe.AsPointer(ref _bufferBarriers[0]),
                imageMemoryBarrierCount = (uint)_imageBarriers.Count,
                pImageMemoryBarriers = (VkImageMemoryBarrier2*)Unsafe.AsPointer(ref _imageBarriers[0])
            };

            Vk.vkCmdPipelineBarrier2(commandBuffer, &dependencyInfo);

            Clear();

            /*Vk.vkCmdPipelineBarrier(
                commandBuffer,
                srcStageFlags,
                dstStageFlags,
                VkDependencyFlags.None,
                0,
                null,
                (uint)_bufferBarriers.Count,
                (VkBufferMemoryBarrier*)Unsafe.AsPointer(ref _bufferBarriers[0]),
                (uint)_imageBarriers.Count,
                (VkImageMemoryBarrier*)Unsafe.AsPointer(ref _imageBarriers[0]));*/
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            _bufferBarriers.Clear();
            _imageBarriers.Clear();
        }

        internal void AddBufferBarrier(VkBufferMemoryBarrier2 barrier)
        {
            if (_bufferBarriers.Count > 0)
            {
                for (int i = 0; i < _bufferBarriers.Count; i++)
                {
                    ref VkBufferMemoryBarrier2 data = ref _bufferBarriers[i];
                    if (data.buffer == barrier.buffer)
                    {
                        _bufferBarriers[i] = barrier;
                        return;
                    }
                }
            }

            _bufferBarriers.Add(barrier);
        }

        internal void AddImageBarrier(VkImageMemoryBarrier2 barrier)
        {
            if (_imageBarriers.Count > 0)
            {
                for (int i = 0; i < _imageBarriers.Count; i++)
                {
                    ref VkImageMemoryBarrier2 data = ref _imageBarriers[i];
                    if (data.image == barrier.image)
                    {
                        _imageBarriers[i] = barrier;
                        return;
                    }
                }
            }

            _imageBarriers.Add(barrier);
        }
    }
}