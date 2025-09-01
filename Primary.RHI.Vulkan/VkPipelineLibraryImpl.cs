using System.Runtime.CompilerServices;
using Vortice.Vulkan;

using Vk = Vortice.Vulkan.Vulkan;

namespace Primary.RHI.Vulkan
{
    internal unsafe sealed class VkPipelineLibraryImpl : PipelineLibrary
    {
        private readonly VkGraphicsDeviceImpl _device;

        private VkPipelineCache _pipelineCache;

        internal VkPipelineLibraryImpl(VkGraphicsDeviceImpl device, Span<byte> initialData)
        {
            _device = device;

            VkPipelineCacheCreateInfo createInfo = new()
            {
                sType = VkStructureType.PipelineCacheCreateInfo,
                pNext = null,
                flags = VkPipelineCacheCreateFlags.None,
                initialDataSize = (nuint)initialData.Length,
                pInitialData = initialData.IsEmpty ? null : Unsafe.AsPointer(ref initialData[0])
            };

            Vk.vkCreatePipelineCache(device.VkDevice, createInfo, null, out _pipelineCache).CheckResult();
        }

        public override void Dispose()
        {
            Vk.vkDestroyPipelineCache(_device.VkDevice, _pipelineCache);
        }

        public override void GetPipelineLibraryData(Span<byte> data)
        {
            uint dataSize = 0;
            Vk.vkGetPipelineCacheData(_device.VkDevice, _pipelineCache, (nuint*)&dataSize, null).CheckResult();

            if ((long)dataSize != data.Length)
                throw new ArgumentException("Invalid data size", nameof(data));

            Vk.vkGetPipelineCacheData(_device.VkDevice, _pipelineCache, (nuint*)&dataSize, Unsafe.AsPointer(ref data[0])).CheckResult();
        }

        public override long PipelineDataSize
        {
            get
            {
                uint dataSize = 0;
                Vk.vkGetPipelineCacheData(_device.VkDevice, _pipelineCache, (nuint*)&dataSize, null).CheckResult();

                return (long)dataSize;
            }
        }
    }
}
