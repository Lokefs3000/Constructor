using Serilog;

namespace Primary.RHI.Direct3D12
{
    public static class GraphicsDeviceFactory
    {
        public static GraphicsDevice Create(ILogger logger, GraphicsDeviceDescription description)
        {
            return new GraphicsDeviceImpl(logger, description);
        }

        private static GraphicsDeviceFactoryCreateImpl s_impl = Create;
    }
}
