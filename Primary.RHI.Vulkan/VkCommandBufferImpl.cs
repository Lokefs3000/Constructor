namespace Primary.RHI.Vulkan
{
    internal interface VkCommandBufferImpl
    {
        //internal VkCommandBuffer VkCommandBuffer { get; }
        //internal VkFence SynchronizationFence { get; }

        internal void SubmitToQueue(int priority);
    }
}
