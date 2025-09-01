using Vortice.Vulkan;

using Vk = Vortice.Vulkan.Vulkan;

namespace Primary.RHI.Vulkan.Utility
{
    internal sealed unsafe class SamplerPool : IDisposable
    {
        private readonly VkGraphicsDeviceImpl _device;

        private Dictionary<int, VkSampler> _samplers;

        private bool _disposedValue;

        internal SamplerPool(VkGraphicsDeviceImpl device)
        {
            _device = device;

            _samplers = new Dictionary<int, VkSampler>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                foreach (VkSampler sampler in _samplers.Values)
                {
                    Vk.vkDestroySampler(_device.VkDevice, sampler);
                }

                _disposedValue = true;
            }
        }

        ~SamplerPool()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public VkSampler Get(ImmutableSamplerDescription samplerDescription)
        {
            int hashCode = samplerDescription.GetHashCode();
            if (_samplers.TryGetValue(hashCode, out VkSampler sampler))
            {
                return sampler;
            }

            VkSamplerCreateInfo createInfo = new()
            {
                sType = VkStructureType.SamplerCreateInfo,
                pNext = null,
                flags = VkSamplerCreateFlags.None,
                minFilter = (samplerDescription.MinificationFilter == TextureFilter.Linear) ? VkFilter.Linear : VkFilter.Nearest,
                magFilter = (samplerDescription.MagnificationFilter == TextureFilter.Linear) ? VkFilter.Linear : VkFilter.Nearest,
                mipmapMode = (samplerDescription.Mode == MipmapMode.Linear) ? VkSamplerMipmapMode.Linear : VkSamplerMipmapMode.Nearest,
                addressModeU = Translate(samplerDescription.AddressModeU),
                addressModeV = Translate(samplerDescription.AddressModeV),
                addressModeW = Translate(samplerDescription.AddressModeW),
                anisotropyEnable = samplerDescription.MaxAnistropy > 0,
                borderColor = VkBorderColor.FloatTransparentBlack,
                compareEnable = false,
                compareOp = VkCompareOp.Never,
                maxAnisotropy = samplerDescription.MaxAnistropy,
                maxLod = samplerDescription.MaxLOD,
                minLod = samplerDescription.MinLOD,
                mipLodBias = samplerDescription.MipLODBias,
                unnormalizedCoordinates = false
            };

            Vk.vkCreateSampler(_device.VkDevice, createInfo, null, out sampler);

            _samplers[hashCode] = sampler;
            return sampler;
        }

        private static VkSamplerAddressMode Translate(TextureAddressMode addressMode)
        {
            switch (addressMode)
            {
                case TextureAddressMode.Repeat: return VkSamplerAddressMode.Repeat;
                case TextureAddressMode.Mirror: return VkSamplerAddressMode.MirroredRepeat;
                case TextureAddressMode.ClampToEdge: return VkSamplerAddressMode.ClampToEdge;
                case TextureAddressMode.ClampToBorder: return VkSamplerAddressMode.ClampToBorder;
            }

            throw new ArgumentException(nameof(addressMode));
        }
    }
}
