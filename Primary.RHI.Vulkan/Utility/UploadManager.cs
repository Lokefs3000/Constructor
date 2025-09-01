using System.Runtime.CompilerServices;
using Vortice.Vulkan;

using Vk = Vortice.Vulkan.Vulkan;
using Vma = Vortice.Vulkan.Vma;

namespace Primary.RHI.Vulkan.Utility
{
    internal unsafe sealed class UploadManager
    {
        private VkGraphicsDeviceImpl _graphicsDevice;

        private SemaphoreSlim _semaphore;

        internal UploadManager(VkGraphicsDeviceImpl graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;

            _semaphore = new SemaphoreSlim(4);
        }

        private VkCommandBuffer GetCommandBuffer()
        {
            VkCommandPool commandPool = _graphicsDevice.TransferCommandPool;
            Vk.vkAllocateCommandBuffer(_graphicsDevice.VkDevice, commandPool, out VkCommandBuffer commandBuffer).CheckResult();

            return commandBuffer;
        }

        private void ReturnCommandBuffer(VkCommandBuffer commandBuffer)
        {
            VkCommandPool commandPool = _graphicsDevice.TransferCommandPool;
            Vk.vkFreeCommandBuffers(_graphicsDevice.VkDevice, commandPool, commandBuffer);
        }

        //Suballocation from bigger buffer!
        private (VkBuffer, VmaAllocation) AllocateStagingBuffer(uint dataSize)
        {
            VkBufferCreateInfo createInfo = new()
            {
                sType = VkStructureType.BufferCreateInfo,
                pNext = null,
                flags = VkBufferCreateFlags.None,
                size = dataSize,
                usage = VkBufferUsageFlags.TransferSrc,
                sharingMode = VkSharingMode.Exclusive,
                pQueueFamilyIndices = null,
                queueFamilyIndexCount = 0,
            };

            VmaAllocationCreateInfo allocInfo = new()
            {
                flags = VmaAllocationCreateFlags.HostAccessSequentialWrite,
                pUserData = null,
                memoryTypeBits = 0,
                pool = VmaPool.Null,
                preferredFlags = VkMemoryPropertyFlags.None,
                priority = 0.0f,
                requiredFlags = VkMemoryPropertyFlags.None,
                usage = VmaMemoryUsage.AutoPreferHost
            };

            Vma.vmaCreateBuffer(_graphicsDevice.VmaAllocator, createInfo, allocInfo, out VkBuffer buffer, out VmaAllocation allocation).CheckResult();
            return (buffer, allocation);
        }

        private void FreeStagingBuffer(VkBuffer buffer, VmaAllocation allocation)
        {
            Vma.vmaDestroyBuffer(_graphicsDevice.VmaAllocator, buffer, allocation);
        }

        internal void UploadInitialBuffer(VkBuffer buffer, VmaAllocation allocation, VkAccessFlags2 accessFlags, VkPipelineStageFlags2 stageFlags, ref BufferDescription description, nint dataPointer)
        {
            VkCommandBuffer commandBuffer = VkCommandBuffer.Null;
            VkBuffer stagingBuffer = VkBuffer.Null;
            VmaAllocation stagingAllocation = VmaAllocation.Null;

            _semaphore.Wait();

            commandBuffer = GetCommandBuffer();

            Vk.vkBeginCommandBuffer(commandBuffer, VkCommandBufferUsageFlags.OneTimeSubmit);

            if (dataPointer != nint.Zero)
            {
                if (description.Memory == MemoryUsage.Dynamic)
                {
                    void* mapped;
                    Vma.vmaMapMemory(_graphicsDevice.VmaAllocator, stagingAllocation, &mapped);
                    Unsafe.CopyBlockUnaligned(mapped, dataPointer.ToPointer(), description.ByteWidth);
                    Vma.vmaUnmapMemory(_graphicsDevice.VmaAllocator, stagingAllocation);
                }
                else
                {
                    (stagingBuffer, stagingAllocation) = AllocateStagingBuffer(description.ByteWidth);

                    void* mapped;
                    Vma.vmaMapMemory(_graphicsDevice.VmaAllocator, stagingAllocation, &mapped);
                    Unsafe.CopyBlockUnaligned(mapped, dataPointer.ToPointer(), description.ByteWidth);

                    VkBufferMemoryBarrier memoryBarrier = new(stagingBuffer, VkAccessFlags.HostWrite, VkAccessFlags.TransferRead);
                    Vk.vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.Host, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 1, &memoryBarrier, imageMemoryBarrierCount: 0, imageMemoryBarriers: null);

                    VkBufferCopy bufferCopy = new()
                    {
                        srcOffset = 0,
                        dstOffset = 0,
                        size = description.ByteWidth,
                    };

                    Vk.vkCmdCopyBuffer(commandBuffer, stagingBuffer, buffer, 1, &bufferCopy);
                }
            }
            else
            {
                Vk.vkCmdFillBuffer(commandBuffer, buffer, 0, description.ByteWidth, 0);
            }

            VkBufferMemoryBarrier2 bufferMemoryBarrier = new(buffer, VkPipelineStageFlags2.Transfer, VkAccessFlags2.TransferWrite, stageFlags, accessFlags);
            VkDependencyInfo dependencyInfo = new() { pBufferMemoryBarriers = &bufferMemoryBarrier, bufferMemoryBarrierCount = 1 };
            Vk.vkCmdPipelineBarrier2(commandBuffer, &dependencyInfo);

            Vk.vkEndCommandBuffer(commandBuffer);

            VkSubmitInfo submitInfo = new()
            {
                sType = VkStructureType.SubmitInfo,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer
            };

            lock (_graphicsDevice.QueueLock)
            {
                Vk.vkQueueSubmit(_graphicsDevice.TransferQueue, 1, &submitInfo, VkFence.Null);
                Vk.vkQueueWaitIdle(_graphicsDevice.TransferQueue);
            }

            ReturnCommandBuffer(commandBuffer);
            FreeStagingBuffer(stagingBuffer, stagingAllocation);

            _semaphore.Release();
        }
    }
}
