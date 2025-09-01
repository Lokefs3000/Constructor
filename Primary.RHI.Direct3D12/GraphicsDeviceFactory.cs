using Serilog;

namespace Primary.RHI.Direct3D12
{
    public static class GraphicsDeviceFactory
    {
        public static GraphicsDevice Create(ILogger logger)
        {
            return new GraphicsDeviceImpl(logger);
        }

        private static GraphicsDeviceFactoryCreateImpl s_impl = Create;
    }
}
