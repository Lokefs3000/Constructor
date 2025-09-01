using CommunityToolkit.HighPerformance;
using System.Buffers;
using System.Runtime.CompilerServices;
using Vortice.Vulkan;
using Vk = Vortice.Vulkan.Vulkan;

namespace Primary.RHI.Vulkan.Utility
{
    internal unsafe class ImageTransitionManager
    {
        private VkGraphicsDeviceImpl _graphicsDevice;

        private List<ImageTransition> _queuedTransitions;

        internal ImageTransitionManager(VkGraphicsDeviceImpl graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;

            _queuedTransitions = new List<ImageTransition>();
        }

        internal void AddToTransitionQueue(ref TransitionableImage data, VkImageLayout newLayout, VkImageSubresourceRange range)
        {
            ReadOnlySpan<ImageTransition> transitions = _queuedTransitions.AsSpan();
            for (int i = 0; i < transitions.Length; i++)
            {
                ref ImageTransition transition = ref transitions.DangerousGetReferenceAt(i);
                if (transition.Image->Image == data.Image)
                {
                    transition.NewLayout = newLayout;
                    transition.Range = range;
                    return;
                }
            }

            if (newLayout == data.CurrentLayout)
                return;

            _queuedTransitions.Add(new ImageTransition
            {
                Image = (TransitionableImage*)Unsafe.AsPointer(ref data),
                NewLayout = newLayout,
                Range = range
            });
        }

        internal void FlushTransitionQueue()
        {
            ReadOnlySpan<ImageTransition> transitions = _queuedTransitions.AsSpan();

            int dataSize = Unsafe.SizeOf<VkHostImageLayoutTransitionInfo>() * _queuedTransitions.Count;
            bool usesArrays = dataSize > 2048;

            VkHostImageLayoutTransitionInfo[]? array = usesArrays ?
                ArrayPool<VkHostImageLayoutTransitionInfo>.Shared.Rent(_queuedTransitions.Count) :
                null;
            Span<VkHostImageLayoutTransitionInfo> span = array ?? stackalloc VkHostImageLayoutTransitionInfo[_queuedTransitions.Count];

            int validTransitionCount = 0;
            for (int i = 0; i < _queuedTransitions.Count; i++)
            {
                ref ImageTransition transition = ref transitions.DangerousGetReferenceAt(i);
                if (transition.Image->CurrentLayout != transition.NewLayout)
                {
                    span[validTransitionCount] = new VkHostImageLayoutTransitionInfo
                    {
                        sType = VkStructureType.HostImageLayoutTransitionInfo,
                        pNext = null,
                        image = transition.Image->Image,
                        oldLayout = transition.Image->CurrentLayout,
                        newLayout = transition.NewLayout,
                        subresourceRange = transition.Range
                    };

                    validTransitionCount++;
                }
            }

            if (validTransitionCount > 0)
            {
                Vk.vkTransitionImageLayout(_graphicsDevice.VkDevice, (uint)validTransitionCount, (VkHostImageLayoutTransitionInfo*)Unsafe.AsPointer(ref span[0]));
            }

            if (usesArrays)
                ArrayPool<VkHostImageLayoutTransitionInfo>.Shared.Return(array!);
        }

        private struct ImageTransition
        {
            public TransitionableImage* Image;
            public VkImageLayout NewLayout;
            public VkImageSubresourceRange Range;
        }
    }

    internal struct TransitionableImage
    {
        private VkImage _image;
        private VkImageLayout _current;

        public TransitionableImage(VkImage image, VkImageLayout current)
        {
            _image = image;
            _current = current;
        }

        /// <summary>
        /// Underlying image object
        /// </summary>
        public VkImage Image => _image;

        /// <summary>
        /// The current layout of the image.
        /// Not guranteed to have changed until a transition flush is invoked.
        /// </summary>
        public VkImageLayout CurrentLayout => _current;

        public static explicit operator VkImage(in TransitionableImage image) => image.Image;
        public static explicit operator VkImageLayout(in TransitionableImage image) => image.CurrentLayout;
    }
}
