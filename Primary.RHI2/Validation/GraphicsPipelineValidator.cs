using Serilog;

namespace Primary.RHI2.Validation
{
    public static class GraphicsPipelineValidator
    {
        public static bool Validate(ref readonly RHIGraphicsPipelineDescription description, ref readonly RHIGraphicsPipelineBytecode bytecode, ILogger? logger, string? resourceName)
        {
            resourceName ??= "GraphicsPipeline";

            for (int i = 0; i < description.InputElements.Length; ++i)
            {
                ref readonly RHIGPInputElement ie = ref description.InputElements[i];

                if (ie.InputSlotClass == RHIInputClass.PerVertex)
                {
                    if (ie.InstanceDataStepRate != 0)
                    {
                        logger?.Error("[r:{n}]: Input element instance data step rate must be 0 when class is per vertex", resourceName);
                        return false;
                    }
                }
                else
                {
                    if (ie.InstanceDataStepRate < 1)
                    {
                        logger?.Error("[r:{n}]: Input element instance data step rate must be more then 0 when class is per instance", resourceName);
                        return false;
                    }
                }
                
            }

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

            if (bytecode.Vertex.IsEmpty)
            {
                logger?.Error("[r:{n}]: Must provide bytecode for vertex shader stage", resourceName);
                return false;
            }

            if (bytecode.Pixel.IsEmpty)
            {
                logger?.Error("[r:{n}]: Must provide bytecode for pixel shader stage", resourceName);
                return false;
            }

            return true;
        }
    }
}
