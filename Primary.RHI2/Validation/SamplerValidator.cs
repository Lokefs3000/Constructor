using Serilog;

namespace Primary.RHI2.Validation
{
    public static class SamplerValidator
    {
        public static bool Validate(ref readonly RHISamplerDescription description, ILogger? logger, string? resourceName)
        {
            resourceName ??= "Sampler";

            if (description.MaxAnisotropy < 1 || description.MaxAnisotropy > 16)
            {
                logger?.Error("[r:{n}]: Sampler anisotropy must be at minimum 1 and at maximum 16", resourceName);
                return false;
            }

            return true;
        }
    }
}
