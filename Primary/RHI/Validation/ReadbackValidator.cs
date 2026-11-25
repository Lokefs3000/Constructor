using Primary.Common;
using Serilog;

namespace Primary.RHI.Validation
{
    public static class ReadbackValidator
    {
        public static bool Validate(ref readonly RHIReadbackDescription description, ILogger? logger, string? resourceName)
        {
            resourceName ??= "Buffer";

            if (description.Width == 0)
            {
                logger?.Error("[r:{n}]: Readback width must be more then 0", resourceName);
                return false;
            }

            return true;
        }
    }
}
