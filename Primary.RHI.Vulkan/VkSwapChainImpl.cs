using Primary.RHI.Vulkan.Utility;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.Vulkan;

using Vk = Vortice.Vulkan.Vulkan;

namespace Primary.RHI.Vulkan
{
    internal unsafe sealed class VkSwapChainImpl : SwapChain
    {
        private readonly VkGraphicsDeviceImpl _graphicsDevice;

        private Vector2 _clientSize;

        private VkSurfaceKHR _surface;
        private VkSwapchainKHR _swapChain;
        private TransitionableImage[] _images;
        private VkImageView[] _imageViews;
        private VkFence _fence;

        private uint _currentSwapchainImage;

        private bool _disposedValue;

        internal VkSwapChainImpl(VkGraphicsDeviceImpl graphicsDevice, Vector2 clientSize, nint windowHandle)
        {
            _graphicsDevice = graphicsDevice;
            _clientSize = clientSize;

            _currentSwapchainImage = 0;

            {
                VkWin32SurfaceCreateInfoKHR createInfo = new()
                {
                    sType = VkStructureType.Win32SurfaceCreateInfoKHR,
                    pNext = null,
                    flags = VkWin32SurfaceCreateFlagsKHR.None,
                    hinstance = nint.Zero,
                    hwnd = windowHandle
                };

                Vk.vkCreateWin32SurfaceKHR(graphicsDevice.VkInstance, createInfo, null, out _surface).CheckResult();
            }

            {
                QueueFamily family = graphicsDevice.QueueFamily;

                uint graphicsFamily = family.GraphicsFamily!.Value;

                VkSwapchainCreateInfoKHR createInfo = new()
                {
                    sType = VkStructureType.SwapchainCreateInfoKHR,
                    pNext = null,
                    flags = VkSwapchainCreateFlagsKHR.None,
                    surface = _surface,
                    minImageCount = 2,
                    imageFormat = VkFormat.R8G8B8A8Unorm,
                    imageColorSpace = VkColorSpaceKHR.SrgbNonLinear,
                    imageExtent = new VkExtent2D((int)clientSize.X, (int)clientSize.Y),
                    imageArrayLayers = 1,
                    imageUsage = VkImageUsageFlags.ColorAttachment,
                    imageSharingMode = VkSharingMode.Exclusive,
                    queueFamilyIndexCount = 1,
                    pQueueFamilyIndices = &graphicsFamily,
                    preTransform = VkSurfaceTransformFlagsKHR.Identity,
                    compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque,
                    presentMode = VkPresentModeKHR.Mailbox,
                    clipped = false,
                    oldSwapchain = VkSwapchainKHR.Null,
                };

                Vk.vkCreateSwapchainKHR(graphicsDevice.VkDevice, createInfo, null, out _swapChain).CheckResult();
            }

            {
                ReadOnlySpan<VkImage> images = Vk.vkGetSwapchainImagesKHR(graphicsDevice.VkDevice, _swapChain);
                _images = new TransitionableImage[images.Length];

                for (int i = 0; i < images.Length; i++)
                {
                    _images[i] = new TransitionableImage(images[i], VkImageLayout.Undefined);
                }
            }

            {
                _imageViews = new VkImageView[_images.Length];

                for (int i = 0; i < _imageViews.Length; i++)
                {
                    VkImage image = _images[i].Image;

                    VkImageViewCreateInfo createInfo = new()
                    {
                        sType = VkStructureType.ImageViewCreateInfo,
                        pNext = null,
                        flags = VkImageViewCreateFlags.None,
                        image = image,
                        viewType = VkImageViewType.Image2D,
                        format = VkFormat.R8G8B8A8Unorm,
                        components = VkComponentMapping.Identity,
                        subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, levelCount: 1, layerCount: 1)
                    };

                    Vk.vkCreateImageView(graphicsDevice.VkDevice, createInfo, null, out _imageViews[i]).CheckResult();
                }
            }

            {
                Vk.vkCreateFence(graphicsDevice.VkDevice, VkFenceCreateFlags.None, out _fence).CheckResult();
            }

            Vk.vkAcquireNextImageKHR(_graphicsDevice.VkDevice, _swapChain, ulong.MaxValue, VkSemaphore.Null, _fence, out _currentSwapchainImage).CheckResult();
            Vk.vkWaitForFences(_graphicsDevice.VkDevice, _fence, true, ulong.MaxValue).CheckResult();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                Vk.vkDestroyFence(_graphicsDevice.VkDevice, _fence);
                foreach (VkImageView imageView in _imageViews)
                    Vk.vkDestroyImageView(_graphicsDevice.VkDevice, imageView);
                Vk.vkDestroySwapchainKHR(_graphicsDevice.VkDevice, _swapChain);
                Vk.vkDestroySurfaceKHR(_graphicsDevice.VkInstance, _surface);

                _disposedValue = true;
            }
        }

        ~VkSwapChainImpl()
        {
            Dispose(disposing: false);
        }

        public override void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override void Present(PresentParameters parameters)
        {
            _graphicsDevice.SchedulePresent(this, parameters);

            /*VkResult result = Vk.vkQueuePresentKHR(_graphicsDevice.GraphicsQueue, VkSemaphore.Null, _swapChain, _currentSwapchainImage);
            if (result == VkResult.ErrorOutOfDateKHR || result == VkResult.SuboptimalKHR)
                EnqueueRecreateSuboptimalSwapchain();
            else
                result.CheckResult();

            Vk.vkResetFences(_graphicsDevice.VkDevice, _fence);
            Vk.vkAcquireNextImageKHR(_graphicsDevice.VkDevice, _swapChain, ulong.MaxValue, VkSemaphore.Null, _fence, out _currentSwapchainImage).CheckResult();
            Vk.vkWaitForFences(_graphicsDevice.VkDevice, _fence, true, ulong.MaxValue).CheckResult();*/
        }

        internal void AcquireNextImage(VkSemaphore semaphore)
        {
            //Vk.vkWaitForFences(_graphicsDevice.VkDevice, _fence, true, 2000).CheckResult();
            //Vk.vkResetFences(_graphicsDevice.VkDevice, _fence);

            Vk.vkAcquireNextImageKHR(_graphicsDevice.VkDevice, _swapChain, 2000, semaphore, VkFence.Null, out _currentSwapchainImage).CheckResult();
        }

        internal void PresentInternalSemaphore(PresentParameters parameters, VkSemaphore semaphore)
        {
            VkResult result = Vk.vkQueuePresentKHR(_graphicsDevice.GraphicsQueue, VkSemaphore.Null, _swapChain, _currentSwapchainImage);
            if (result == VkResult.ErrorOutOfDateKHR || result == VkResult.SuboptimalKHR)
                EnqueueRecreateSuboptimalSwapchain();
            else
                result.CheckResult();
        }

        public override void Resize(Vector2 newClientSize)
        {
            if (_clientSize == newClientSize)
            {
                _clientSize = newClientSize;
                _graphicsDevice.EnqueueActionForNextFrame(RecreateSwapchain);
            }
        }

        private void RecreateSwapchain()
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnqueueRecreateSuboptimalSwapchain() => _graphicsDevice.EnqueueActionForNextFrame(RecreateSwapchain);

        public override Vector2 ClientSize => _clientSize;
    }
}
