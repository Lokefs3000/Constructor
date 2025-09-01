using Primary.RHI.Vulkan.Utility;
using Vortice.Vulkan;

using Vk = Vortice.Vulkan.Vulkan;

namespace Primary.RHI.Vulkan
{
    internal unsafe sealed class VkTransferCommandBufferImpl : CopyCommandBuffer, VkCommandBufferImpl
    {
        private VkGraphicsDeviceImpl _device;

        private VkCommandBuffer _commandBuffer;

        private PipelineBarrierManager _barrierManager;

        private bool _isOpen;
        private bool _disposedValue;

        internal VkTransferCommandBufferImpl(VkGraphicsDeviceImpl device)
        {
            _device = device;

            //Vk.vkAllocateCommandBuffer(_device.VkDevice, device.TransferCommandPool, out _commandBuffer);

            _barrierManager = new PipelineBarrierManager();

            _isOpen = false;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _barrierManager.Dispose();
                }

                if (_isOpen)
                    Vk.vkEndCommandBuffer(_commandBuffer);
                if (_commandBuffer.IsNotNull)
                    _device.ReturnCommandBuffer(_commandBuffer, CommandBufferType.Transfer);

                _disposedValue = true;
            }
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        void VkCommandBufferImpl.SubmitToQueue(int priority)
        {
            _device.EnqueueCommandBuffer(_commandBuffer, CommandBufferType.Transfer, priority);
            _commandBuffer = VkCommandBuffer.Null;
        }

        public override void Begin()
        {
            if (!_isOpen)
            {
                if (_commandBuffer.IsNotNull)
                    _device.ReturnCommandBuffer(_commandBuffer, CommandBufferType.Transfer);
                _commandBuffer = _device.GetCommandBuffer(CommandBufferType.Transfer);

                Vk.vkBeginCommandBuffer(_commandBuffer, VkCommandBufferUsageFlags.OneTimeSubmit);

                _barrierManager.Clear();

                _isOpen = true;
            }
        }

        public override void Close()
        {
            if (_isOpen)
            {
                Vk.vkEndCommandBuffer(_commandBuffer);

                _isOpen = false;
            }
        }

        public override void CopyBufferRegion(Buffer src, uint srcOffset, Buffer dst, uint dstOffset, uint size)
        {
            VkBufferImpl srcImpl = (VkBufferImpl)src;
            VkBufferImpl dstImpl = (VkBufferImpl)dst;

            srcImpl.TransitionToSrcCopy(_barrierManager);
            dstImpl.TransitionToDstCopy(_barrierManager);

            _barrierManager.TransitionPending(_commandBuffer);

            VkBufferCopy region = new()
            {
                srcOffset = srcOffset,
                dstOffset = dstOffset,
                size = size
            };

            Vk.vkCmdCopyBuffer(_commandBuffer, ((VkBufferImpl)src).VkBuffer, ((VkBufferImpl)src).VkBuffer, 1, &region);
        }

        public override void CopyTextureRegion(Resource src, TextureLocation srcLoc, uint srcSubRes, Resource dst, TextureLocation dstLoc, uint dstSubRes)
        {
            throw new NotImplementedException();
        }

        public override nint Map(Resource resource, MapIntent intent, MapRange? readRange)
        {
            if (resource is VkBufferImpl impl)
            {
                impl.TransitionToMap(_barrierManager, intent);
                _barrierManager.TransitionPending(_commandBuffer);

                return impl.Map(intent, readRange);
            }

            throw new NotImplementedException();
        }

        public override void Unmap(Resource resource, MapRange? writeRange)
        {
            if (resource is VkBufferImpl impl)
            {
                impl.Unmap(writeRange);
                return;
            }

            throw new NotImplementedException();
        }

        public override bool IsOpen => _isOpen;
        public override CommandBufferType Type => CommandBufferType.Transfer;
    }
}
