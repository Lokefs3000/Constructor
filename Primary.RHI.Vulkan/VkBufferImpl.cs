using Primary.RHI.Vulkan.Utility;
using System.Runtime.CompilerServices;
using Vortice.Vulkan;

using Vk = Vortice.Vulkan.Vulkan;
using Vma = Vortice.Vulkan.Vma;

namespace Primary.RHI.Vulkan
{
    internal unsafe sealed class VkBufferImpl : Buffer
    {
        private readonly VkGraphicsDeviceImpl _graphicsDevice;
        private readonly BufferDescription _description;

        private VkBuffer _buffer;
        private VmaAllocation _allocation;
        private VkBufferView _bufferView;
        private VkAccessFlags2 _accessFlags;

        private VkAccessFlags2 _currentAccessFlags;
        private VkPipelineStageFlags2 _prevStageFlags;

        private nint _activeMapPointer;
        private MapIntent _intent;

        private bool _disposedValue;

        internal VkBufferImpl(VkGraphicsDeviceImpl graphicsDevice, ref BufferDescription description, nint dataPointer)
        {
            _graphicsDevice = graphicsDevice;
            _description = description;

            bool hasUploadPending = dataPointer != nint.Zero && description.ByteWidth > 0;

            VkBufferUsageFlags bufferUsage = VkBufferUsageFlags.None;

            if (description.Usage.HasFlag(BufferUsage.VertexBuffer))
                bufferUsage |= VkBufferUsageFlags.VertexBuffer;
            if (description.Usage.HasFlag(BufferUsage.IndexBuffer))
                bufferUsage |= VkBufferUsageFlags.IndexBuffer;
            if (description.Usage.HasFlag(BufferUsage.ConstantBuffer))
                bufferUsage |= VkBufferUsageFlags.UniformBuffer;
            if (description.Usage.HasFlag(BufferUsage.ShaderResource))
                bufferUsage |= VkBufferUsageFlags.StorageBuffer;

            _accessFlags = VkAccessFlags2.None;
            if (dataPointer != nint.Zero && description.ByteWidth > 0)
                _accessFlags |= VkAccessFlags2.TransferWrite;
            if (description.Usage.HasFlag(BufferUsage.VertexBuffer))
                _accessFlags |= VkAccessFlags2.VertexAttributeRead;
            if (description.Usage.HasFlag(BufferUsage.IndexBuffer))
                _accessFlags |= VkAccessFlags2.IndexRead;
            if (description.Usage.HasFlag(BufferUsage.ConstantBuffer))
                _accessFlags |= VkAccessFlags2.UniformRead;
            if (description.Usage.HasFlag(BufferUsage.ShaderResource))
                _accessFlags |= VkAccessFlags2.ShaderRead;
            if (description.Mode.HasFlag(BufferMode.Unordered))
                _accessFlags |= VkAccessFlags2.ShaderWrite;

            VmaAllocationCreateFlags flags = VmaAllocationCreateFlags.None;
            if (description.Memory == MemoryUsage.Default)
            {
                flags |= VmaAllocationCreateFlags.HostAccessAllowTransferInstead;
                bufferUsage |= VkBufferUsageFlags.TransferDst;
            }
            if (description.Memory != MemoryUsage.Immutable)
                flags |= VmaAllocationCreateFlags.HostAccessSequentialWrite;

            VmaMemoryUsage memoryUsage = description.Memory switch
            {
                MemoryUsage.Immutable => VmaMemoryUsage.AutoPreferDevice,
                MemoryUsage.Default => VmaMemoryUsage.Auto,
                MemoryUsage.Dynamic => VmaMemoryUsage.Auto,
                MemoryUsage.Staging => VmaMemoryUsage.AutoPreferHost,
                _ => throw new ArgumentException(description.Memory.ToString(), "description.Memory")
            };

            VkBufferCreateInfo createInfo = new()
            {
                sType = VkStructureType.BufferCreateInfo,
                pNext = null,
                flags = VkBufferCreateFlags.None,
                size = description.ByteWidth,
                usage = bufferUsage | (hasUploadPending ? VkBufferUsageFlags.TransferDst : VkBufferUsageFlags.None),
                sharingMode = VkSharingMode.Exclusive,
                queueFamilyIndexCount = 0,
                pQueueFamilyIndices = null,
            };

            VmaAllocationCreateInfo allocationInfo = new()
            {
                flags = flags,
                usage = memoryUsage,
                requiredFlags = VkMemoryPropertyFlags.None,
                preferredFlags = VkMemoryPropertyFlags.None,
                memoryTypeBits = 0,
                pool = VmaPool.Null,
                pUserData = null,
                priority = 0.0f
            };

            Vma.vmaCreateBuffer(graphicsDevice.VmaAllocator, createInfo, allocationInfo, out _buffer, out _allocation).CheckResult();

            _prevStageFlags = VkPipelineStageFlags2.None;
            if (description.Usage.HasFlag(BufferUsage.VertexBuffer))
                _prevStageFlags = VkPipelineStageFlags2.VertexInput;
            if (description.Usage.HasFlag(BufferUsage.IndexBuffer))
                _prevStageFlags = VkPipelineStageFlags2.VertexInput;
            if (description.Usage.HasFlag(BufferUsage.ConstantBuffer))
                _prevStageFlags = VkPipelineStageFlags2.VertexShader | VkPipelineStageFlags2.FragmentShader;
            if (description.Usage.HasFlag(BufferUsage.ShaderResource))
                _prevStageFlags = VkPipelineStageFlags2.VertexShader | VkPipelineStageFlags2.FragmentShader;
            if (description.Mode.HasFlag(BufferMode.Unordered))
                _prevStageFlags = VkPipelineStageFlags2.ComputeShader;

            if (hasUploadPending)
            {
                _accessFlags &= ~VkAccessFlags2.TransferWrite;
                _graphicsDevice.UploadManager.UploadInitialBuffer(_buffer, _allocation, _accessFlags, _prevStageFlags, ref description, dataPointer);

                createInfo.usage = bufferUsage;

                Vk.vkDestroyBuffer(_graphicsDevice.VkDevice, _buffer);
                Vk.vkCreateBuffer(_graphicsDevice.VkDevice, createInfo, null, out _buffer).CheckResult();
                Vma.vmaBindBufferMemory2(_graphicsDevice.VmaAllocator, _allocation, 0, _buffer, null).CheckResult();
            }

            if (false && (description.Usage.HasFlag(BufferUsage.ConstantBuffer) || description.Usage.HasFlag(BufferUsage.ShaderResource)))
            {
                VkBufferViewCreateInfo viewCreateInfo = new()
                {
                    sType = VkStructureType.BufferViewCreateInfo,
                    pNext = null,
                    flags = VkBufferViewCreateFlags.None,
                    buffer = _buffer,
                    format = VkFormat.Undefined,
                    offset = 0,
                    range = Vk.VK_WHOLE_SIZE
                };

                Vk.vkCreateBufferView(_graphicsDevice.VkDevice, viewCreateInfo, null, out _bufferView).CheckResult();
            }
            else
            {
                _bufferView = VkBufferView.Null;
            }

            _currentAccessFlags = _accessFlags;

            _activeMapPointer = nint.Zero;
            _intent = MapIntent.Read;

            _disposedValue = false;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (_activeMapPointer != nint.Zero)
                {
                    //bad
                    //report but still run
                }

                if (_bufferView.IsNotNull)
                    Vk.vkDestroyBufferView(_graphicsDevice.VkDevice, _bufferView);
                Vma.vmaDestroyBuffer(_graphicsDevice.VmaAllocator, _buffer, _allocation);

                _disposedValue = true;
            }
        }

        ~VkBufferImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region Memory
        internal nint Map(MapIntent intent, MapRange? readRange)
        {
            if (_activeMapPointer != nint.Zero)
            {
                if (_intent != intent)
                {
                    //bad
                    return nint.Zero;
                }

                return _activeMapPointer;
            }

            void* data = null;
            VkResult result = Vma.vmaMapMemory(_graphicsDevice.VmaAllocator, _allocation, &data);

            if (result != VkResult.Success)
            {
                //bad
                return nint.Zero;
            }

            _intent = intent;
            _activeMapPointer = (nint)data;

            return (nint)data;
        }

        internal void Unmap(MapRange? writeRange)
        {
            if (_activeMapPointer != nint.Zero)
            {
                Vma.vmaUnmapMemory(_graphicsDevice.VmaAllocator, _allocation);

                _activeMapPointer = nint.Zero;
            }
        }
        #endregion

        #region Transitions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TransitionToSrcCopy(PipelineBarrierManager barrierManager)
        {
            if (_currentAccessFlags != VkAccessFlags2.TransferRead)
            {
                barrierManager.AddBufferBarrier(new VkBufferMemoryBarrier2(_buffer, _prevStageFlags, _currentAccessFlags, VkPipelineStageFlags2.Transfer, VkAccessFlags2.TransferRead));
                _prevStageFlags = VkPipelineStageFlags2.Transfer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TransitionToDstCopy(PipelineBarrierManager barrierManager)
        {
            if (_currentAccessFlags != VkAccessFlags2.TransferWrite)
            {
                barrierManager.AddBufferBarrier(new VkBufferMemoryBarrier2(_buffer, _prevStageFlags, _currentAccessFlags, VkPipelineStageFlags2.Transfer, VkAccessFlags2.TransferWrite));
                _prevStageFlags = VkPipelineStageFlags2.Transfer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TransitionToMap(PipelineBarrierManager barrierManager, MapIntent intent)
        {
            VkAccessFlags2 flags = intent switch { MapIntent.Read => VkAccessFlags2.HostRead, MapIntent.Write => VkAccessFlags2.HostWrite, MapIntent.ReadWrite => VkAccessFlags2.HostRead | VkAccessFlags2.HostWrite, _ => throw new ArgumentException(nameof(intent)) };
            if (_currentAccessFlags != flags)
            {
                barrierManager.AddBufferBarrier(new VkBufferMemoryBarrier2(_buffer, _prevStageFlags, _currentAccessFlags, VkPipelineStageFlags2.Host, flags));
                _prevStageFlags = VkPipelineStageFlags2.Host;
            }
        }
        #endregion

        public override BufferDescription Description => _description;

        internal VkBuffer VkBuffer => _buffer;
        internal VmaAllocation VmaAllocation => _allocation;
        internal VkBufferView VkBufferView => _bufferView;
    }
}
