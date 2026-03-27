using Serilog;

namespace Primary.RHI2.Validation
{
    public static class ComputePipelineValidator
    {
        public static bool Validate(ref readonly RHIComputePipelineDescription description, ref readonly RHIComputePipelineBytecode bytecode, ILogger? logger, string? resourceName)
        {
            resourceName ??= "ComputePipeline";

            if (description.Expected32BitConstants > 32)
            {
                logger?.Error("[r:{n}]: Expected 32 bit constants must be within 128 bytes (32 values)", resourceName);
                return false;
            }

            if (description.Header32BitConstants > 32 && !description.UseBufferForHeader)
            {
                logger?.Error("[r:{n}]: Header 32 bit constants must be within 128 bytes (32 values) if not using buffer", resourceName);
                return false;
            }

            if (bytecode.Compute.IsEmpty)
            {
                logger?.Error("[r:{n}]: Must provide bytecode for compute shader stage", resourceName);
                return false;
            }

            return true;
        }
    }
}
