using Serilog;

namespace Primary.RHI2.Validation
{
    public static class SwapChainValidator
    {
        public static bool Validate(ref readonly RHISwapChainDescription description, ILogger? logger, string? resourceName)
        {
            resourceName ??= "SwapChain";

            if (description.WindowHandle == nint.Zero)
            {
                logger?.Error("[r:{n}]: Swap chain window handle must not be null", resourceName);
                return false;
            }

            if (description.WindowSize.X < 1.0f || description.WindowSize.X > 16384.0f)
            {
                logger?.Error("[r:{n}]: Swap chain client width must be more then 0 and less than 16384", resourceName);
                return false;
            }

            if (description.WindowSize.Y < 1.0f || description.WindowSize.Y > 16384.0f)
            {
                logger?.Error("[r:{n}]: Swap chain client height must be more then 0 and less than 16384", resourceName);
                return false;
            }

            if (!description.BackBufferFormat.IsSwapChainCapable())
            {
                logger?.Error("[r:{n}]: Format specified for swap chain back buffers is not compatible", resourceName);
                return false;
            }

            if (description.BackBufferCount < 2)
            {
                logger?.Error("[r:{n}]: Swap chain must have atleast 2 back buffers", resourceName);
                return false;
            }

            return true;
        }
    }
}
