using Primary.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.RHI2.Validation
{
    public static class BufferValidator
    {
        public static bool Validate(ref readonly RHIBufferDescription description, ILogger? logger, string? resourceName)
        {
            resourceName ??= "Buffer";

            if (description.Width == 0)
            {
                logger?.Error("[r:{n}]: Buffer width must be more then 0", resourceName);
                return false;
            }

            if (description.Stride > description.Width)
            {
                logger?.Error("[r:{n}]: Buffer stride must be less then or equal to width", resourceName);
                return false;
            }

            if (description.Stride != 0 && description.Width / description.Stride == 0)
            {
                logger?.Error("[r:{n}]: Buffer width and stride equals zero elements", resourceName);
                return false;
            }

            bool unorderedAccess = FlagUtility.HasFlag(description.Usage, RHIResourceUsage.UnorderedAccess);
            if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.ShaderResource) || unorderedAccess)
            {
                switch (description.Mode)
                {
                    case RHIBufferMode.Default:
                        {
                            long elementCount = (description.Stride == 0 || description.ElementCount > 0) ? description.ElementCount : description.Width / description.Stride;

                            if (description.Stride == 0)
                            {
                                logger?.Error("[r:{n}]: A buffer intended for use as a shader resource/unordered access must have a stride", resourceName);
                                return false;
                            }

                            if (elementCount <= 0)
                            {
                                logger?.Error("[r:{n}]: A buffer intended for use as a shader resource/unordered access must have more then 0 elements", resourceName);
                                return false;
                            }

                            if ((elementCount * description.Stride) + description.FirstElement > description.Width)
                            {
                                logger?.Error("[r:{n}]: A buffer intended for use as a shader resource/unordered access with first element must not overflow end of buffer", resourceName);
                                return false;
                            }

                            break;
                        }
                    case RHIBufferMode.Raw:
                        {
                            long elementCount = (description.Stride == 0 || description.ElementCount > 0) ? description.ElementCount : description.Width;
                            
                            if (description.Stride > 2)
                            {
                                logger?.Error("[r:{n}]: A buffer intended for use as a raw shader resource/unordered access must have a stride of 1 or 0", resourceName);
                                return false;
                            }

                            if (elementCount + description.FirstElement > description.Width)
                            {
                                logger?.Error("[r:{n}]: A buffer intended for use as a raw shader resource/unordered access with first element must not overflow end of buffer", resourceName);
                                return false;
                            }

                            break;
                        }
                }
            }

            if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.ConstantBuffer))
            {
                
            }

            if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.VertexInput))
            {

            }

            if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.IndexInput))
            {

            }

            if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.RenderTarget))
            {
                logger?.Error("[r:{n}]: A buffer cannot be used as a render target", resourceName);
                return false;
            }

            if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.DepthStencil))
            {
                logger?.Error("[r:{n}]: A buffer cannot be used as a render target", resourceName);
                return false;
            }

            return true;
        }
    }
}
