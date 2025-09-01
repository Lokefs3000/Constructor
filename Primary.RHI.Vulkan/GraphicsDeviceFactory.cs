using Serilog;

namespace Primary.RHI.Vulkan
{
    public static class GraphicsDeviceFactory
    {
        public static GraphicsDevice Create()
        {
            return new VkGraphicsDeviceImpl(new LoggerConfiguration().WriteTo.Console().CreateLogger());
        }

        private static GraphicsDeviceFactoryCreateImpl s_impl = Create;
    }
}
