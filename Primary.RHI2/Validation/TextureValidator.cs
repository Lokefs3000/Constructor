using Primary.Common;
using Serilog;

namespace Primary.RHI2.Validation
{
    public static class TextureValidator
    {
        public static bool Validate(ref readonly RHITextureDescription description, ILogger? logger, string? resourceName)
        {
            resourceName ??= "Texture";

            switch (description.Dimension)
            {
                case RHIDimension.Texture1D:
                    {
                        if (description.Width <= 0 || description.Width > 16384)
                        {
                            logger?.Error("[r:{n}]: 1 dimensional texture must have a width larger then 0 and less than 16384", resourceName);
                            return false;
                        }

                        if (description.Height != 1)
                        {
                            logger?.Error("[r:{n}]: 1 dimensional texture must have a height of 1", resourceName);
                            return false;
                        }

                        if (description.DepthOrArraySize < 1 || description.DepthOrArraySize > 2048)
                        {
                            logger?.Error("[r:{n}]: 1 dimensional texture must have an array size of 1 and less than 2048", resourceName);
                            return false;
                        }

                        break;
                    }
                case RHIDimension.TextureCube:
                case RHIDimension.Texture2D:
                    {
                        if (description.Width <= 0 || description.Width > 16384)
                        {
                            logger?.Error("[r:{n}]: 2 dimensional/cube texture must have a width larger then 0 and less than 16384", resourceName);
                            return false;
                        }

                        if (description.Height <= 0 || description.Height > 16384)
                        {
                            logger?.Error("[r:{n}]: 2 dimensional/cube texture must have a height larger then 0 and less than 16384", resourceName);
                            return false;
                        }

                        if (description.DepthOrArraySize < 1 || description.DepthOrArraySize > 2048)
                        {
                            logger?.Error("[r:{n}]: 2 dimensional/cube texture must have an array size of 1 and less than 2048", resourceName);
                            return false;
                        }

                        break;
                    }
                case RHIDimension.Texture3D:
                    {
                        if (description.Width <= 0 || description.Width > 2048)
                        {
                            logger?.Error("[r:{n}]: 3 dimensional texture must have a width larger then 0 and less than 2048", resourceName);
                            return false;
                        }

                        if (description.Height <= 0 || description.Height > 2048)
                        {
                            logger?.Error("[r:{n}]: 3 dimensional texture must have a height larger then 0 and less than 2048", resourceName);
                            return false;
                        }

                        if (description.DepthOrArraySize <= 0 || description.DepthOrArraySize > 2048)
                        {
                            logger?.Error("[r:{n}]: 3 dimensional texture must have a depth larger then 0 and less than 2048", resourceName);
                            return false;
                        }

                        break;
                    }
            }

            int maxMipLevels = RHIDevice.CalculateMaxMipLevels(description.Width, description.Height, description.Dimension == RHIDimension.Texture3D ? description.DepthOrArraySize : 1);
            if (description.MipLevels > maxMipLevels)
            {
                logger?.Error("[r:{n}]: Texture has more mip levels specified than capable", resourceName);
                return false;
            }

            bool unorderedAccess = FlagUtility.HasFlag(description.Usage, RHIResourceUsage.UnorderedAccess);
            if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.ShaderResource))
            {
                if (!description.Format.IsShaderCapable())
                {
                    logger?.Error("[r:{n}]: A texture intended for use as a shader resource/unordered must have a compatible format", resourceName);
                    return false;
                }
            }

            if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.ConstantBuffer))
            {
                logger?.Error("[r:{n}]: A texture cannot be used as a constant buffer", resourceName);
                return false;
            }

            if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.VertexInput))
            {
                logger?.Error("[r:{n}]: A texture cannot be used as a vertex buffer", resourceName);
                return false;
            }

            if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.IndexInput))
            {
                logger?.Error("[r:{n}]: A texture cannot be used as a index buffer", resourceName);
                return false;
            }

            if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.RenderTarget))
            {
                if (description.Dimension != RHIDimension.Texture2D)
                {
                    logger?.Error("[r:{n}]: A texture intended for use as a render target must be 2 dimensional", resourceName);
                    return false;
                }

                if (description.DepthOrArraySize != 1)
                {
                    logger?.Error("[r:{n}]: A texture intended for use as a render target must have an array size of 1", resourceName);
                    return false;
                }

                if (!description.Format.IsRenderTargetCapable())
                {
                    logger?.Error("[r:{n}]: A texture intended for use as a render target must have a compatible format", resourceName);
                    return false;
                }
            }

            if (FlagUtility.HasFlag(description.Usage, RHIResourceUsage.DepthStencil))
            {
                if (description.Dimension != RHIDimension.Texture2D)
                {
                    logger?.Error("[r:{n}]: A texture intended for use as a depth stencil must be 2 dimensional", resourceName);
                    return false;
                }

                if (description.DepthOrArraySize != 1)
                {
                    logger?.Error("[r:{n}]: A texture intended for use as a depth stencil must have an array size of 1", resourceName);
                    return false;
                }

                if (!description.Format.IsDepthStencilCapable())
                {
                    logger?.Error("[r:{n}]: A texture intended for use as a depth stencil must have a compatible format", resourceName);
                    return false;
                }
            }

            return true;
        }
    }
}
