using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI2;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Primary.Rendering
{
    public ref struct RasterPassDescription : IDisposable
    {
        private readonly RenderPass _renderPass;
        private readonly string _name;
        private readonly Type _passDataType;

        private PooledList<UsedResourceData> _usedResources;
        private PooledList<UsedRenderTargetData> _usedRenderTargets;

        private Action<IPassContext, IPassData>? _function;

        private bool _allowCulling;

        internal RasterPassDescription(RenderPass renderPass, string name, Type passDataType)
        {
            _renderPass = renderPass;
            _name = name;
            _passDataType = passDataType;

            _usedResources = new PooledList<UsedResourceData>();
            _usedRenderTargets = new PooledList<UsedRenderTargetData>();

            _function = null;

            _allowCulling = true;
        }

        public void Dispose()
        {
            RenderPass.AddGlobalResources(_usedResources, _usedRenderTargets);
            _renderPass.AddNewRenderPass(new RenderPassDescription(_name, RenderPassType.Graphics, _usedResources, _usedRenderTargets, _passDataType, _function, _allowCulling));
        }

        public FrameGraphTexture CreateTexture(FrameGraphTextureDesc desc, string? debugName = null)
        {
            //validate
            {
                int start = 31 - int.LeadingZeroCount((int)desc.Usage);
                for (int i = start; i >= 0; i--)
                {
                    FGTextureUsage usage = (FGTextureUsage)(1 << i);
                    if (FlagUtility.HasFlag(desc.Usage, usage))
                    {
                        if (FlagUtility.HasFlag(desc.Usage, ~s_textureUsageMap.DangerousGetReferenceAt(i)))
                        {
                            _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.IncompatibleUsage, debugName);
                            return FrameGraphTexture.Invalid;
                        }

                        switch (usage)
                        {
                            case FGTextureUsage.GenericShader:
                            case FGTextureUsage.PixelShader:
                                {
                                    //TODO: Restore format checking for texture formats (with dimension specific checking aswell)

                                    //if (!desc.Format.IsCompatibleWithDimension())
                                    //{
                                    //    _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.IncompatibleFormat, debugName);
                                    //    return FrameGraphTexture.Invalid;
                                    //}

                                    break;
                                }
                            case FGTextureUsage.RenderTarget:
                                {
                                    if (desc.Dimension != FGTextureDimension._2D)
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidDimension, debugName);
                                        return FrameGraphTexture.Invalid;
                                    }

                                    if (desc.Depth != 1)
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize, debugName);
                                        return FrameGraphTexture.Invalid;
                                    }

                                    if (!desc.Format.IsRenderTargetCapable())
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.IncompatibleFormat, debugName);
                                        return FrameGraphTexture.Invalid;
                                    }

                                    break;
                                }
                            case FGTextureUsage.DepthStencil:
                                {
                                    if (desc.Dimension != FGTextureDimension._2D)
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidDimension, debugName);
                                        return FrameGraphTexture.Invalid;
                                    }

                                    if (desc.Depth != 1)
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize, debugName);
                                        return FrameGraphTexture.Invalid;
                                    }

                                    if (!desc.Format.IsDepthStencilCapable())
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.IncompatibleFormat, debugName);
                                        return FrameGraphTexture.Invalid;
                                    }

                                    break;
                                }
                        }
                    }
                }

                switch (desc.Dimension)
                {
                    case FGTextureDimension._1D:
                        {
                            if (!IsWithinRange(desc.Width) || desc.Height != 1 || !IsWithinRangeLayers(desc.Depth))
                            {
                                _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize, debugName);
                                return FrameGraphTexture.Invalid;
                            }
                            break;
                        }
                    case FGTextureDimension._2D:
                        {
                            if (!IsWithinRange(desc.Width) || !IsWithinRange(desc.Height) || !IsWithinRangeLayers(desc.Depth))
                            {
                                _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize, debugName);
                                return FrameGraphTexture.Invalid;
                            }
                            break;
                        }
                    case FGTextureDimension._3D:
                        {
                            if (!IsWithinRange3D(desc.Width) || !IsWithinRange3D(desc.Height) || !IsWithinRange3D(desc.Depth))
                            {
                                _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize, debugName);
                                return FrameGraphTexture.Invalid;
                            }
                            break;
                        }
                    default:
                        {
                            _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidDimension, debugName);
                            return FrameGraphTexture.Invalid;
                        }
                }

                static bool IsWithinRange(int dim) => (uint)dim <= 16384;
                static bool IsWithinRange3D(int dim) => (uint)dim <= 2048;
                static bool IsWithinRangeLayers(int dim) => (uint)dim <= 2048;
            }

            FrameGraphTexture texture = new FrameGraphResource(_renderPass.GetNewResourceIndex(), desc, debugName).AsTexture();
            _renderPass.Manager.Resources.AddFGResource(texture);

            return texture;
        }

        public FrameGraphBuffer CreateBuffer(FrameGraphBufferDesc desc, string? debugName = null)
        {
            //validate
            {
                if (desc.Width == 0)
                {
                    _renderPass.ReportError(RPErrorSource.CreateBuffer, RPErrorType.InvalidSize, debugName);
                    return FrameGraphBuffer.Invalid;
                }

                if (desc.Stride > desc.Width)
                {
                    _renderPass.ReportError(RPErrorSource.CreateBuffer, RPErrorType.StrideTooLarge, debugName);
                    return FrameGraphBuffer.Invalid;
                }

                //if (desc.Stride > 1 && ExMath.FastIntergralSqrt(desc.Width) != desc.Stride)
                //{
                //    _renderPass.ReportError(RPErrorSource.CreateBuffer, RPErrorType.InvalidStride);
                //    return FrameGraphBuffer.Invalid;
                //}

                int start = 31 - int.LeadingZeroCount((int)desc.Usage);
                for (int i = start; i >= 0; i--)
                {
                    FGBufferUsage usage = (FGBufferUsage)(1 << i);
                    if (FlagUtility.HasFlag(desc.Usage, usage))
                    {
                        if (FlagUtility.HasFlag(desc.Usage, ~s_bufferUsageMap.DangerousGetReferenceAt(i)))
                        {
                            _renderPass.ReportError(RPErrorSource.CreateBuffer, RPErrorType.IncompatibleUsage, debugName);
                            return FrameGraphBuffer.Invalid;
                        }
                    }
                }
            }

            //Not a bug but a D3D12 limitation
            if (FlagUtility.HasFlag(desc.Usage, FGBufferUsage.ConstantBuffer))
                desc.Width = Math.Max(desc.Width, 256);

            FrameGraphBuffer buffer = new FrameGraphResource(_renderPass.GetNewResourceIndex(), desc, debugName).AsBuffer();
            _renderPass.Manager.Resources.AddFGResource(buffer);

            return buffer;
        }

        public void UseResource(FGResourceUsage usage, FrameGraphTexture resource)
        {
            //validate
            {
                if (FlagUtility.HasFlag(usage, FGResourceUsage.Read))
                {
                    if (!FlagUtility.HasEither(resource.Description.Usage, FGTextureUsage.GenericShader | FGTextureUsage.PixelShader))
                    {
                        if (!FlagUtility.HasFlag(usage, FGResourceUsage.NoShaderAccess))
                            _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.NoShaderAccess, resource.ToString());
                    }
                }

                if (FlagUtility.HasFlag(usage, FGResourceUsage.Write))
                {
                    if (!FlagUtility.HasEither(resource.Description.Usage, FGTextureUsage.GenericShader | FGTextureUsage.PixelShader))
                    {
                        _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.NoShaderAccess, resource.ToString());
                    }

                    if (!FlagUtility.HasEither(resource.Description.Usage, FGTextureUsage.RenderTarget | FGTextureUsage.DepthStencil))
                    {
                        _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.InvalidUsage, resource.ToString());
                    }
                }
            }

            _usedResources.Add(new UsedResourceData(usage, resource));
        }

        public void UseResource(FGResourceUsage usage, FrameGraphBuffer resource)
        {
            //validate
            {
                switch (usage)
                {
                    case FGResourceUsage.Read:
                        {
                            if (!FlagUtility.HasEither(resource.Description.Usage, FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.VertexBuffer | FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer))
                            {
                                _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.NoShaderAccess, resource.ToString());
                            }

                            break;
                        }
                    case FGResourceUsage.Write:
                        {
                            if (!FlagUtility.HasEither(resource.Description.Usage, FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.VertexBuffer | FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer))
                            {
                                _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.NoShaderAccess, resource.ToString());
                            }

                            break;
                        }
                }
            }

            _usedResources.Add(new UsedResourceData(usage, resource));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UseResource(FGResourceUsage usage, FrameGraphResource resource)
        {
            if (resource.ResourceId == FGResourceId.Texture)
            {
                UseResource(usage, resource.AsTexture());
            }
            else
            {
                Debug.Assert(resource.ResourceId == FGResourceId.Buffer);
                UseResource(usage, resource.AsBuffer());
            }
        }

        public void UseRenderTarget(FrameGraphTexture renderTarget)
        {
            //validate
            {
                if (!renderTarget.Description.Format.IsRenderTargetCapable())
                {
                    _renderPass.ReportError(RPErrorSource.UseRenderTarget, RPErrorType.IncompatibleFormat, renderTarget.ToString());
                    return;
                }

                if (!FlagUtility.HasFlag(renderTarget.Description.Usage, FGTextureUsage.RenderTarget))
                {
                    _renderPass.ReportError(RPErrorSource.UseRenderTarget, RPErrorType.MissingUsageFlag, renderTarget.ToString());
                    return;
                }
            }

            _usedRenderTargets.Add(new UsedRenderTargetData(FGRenderTargetType.RenderTarget, renderTarget));
        }

        public void UseDepthStencil(FrameGraphTexture depthStencil)
        {
            //validate
            {
                if (!depthStencil.Description.Format.IsDepthStencilCapable())
                {
                    _renderPass.ReportError(RPErrorSource.UseDepthStencil, RPErrorType.IncompatibleFormat, depthStencil.ToString());
                    return;
                }

                if (!FlagUtility.HasFlag(depthStencil.Description.Usage, FGTextureUsage.DepthStencil))
                {
                    _renderPass.ReportError(RPErrorSource.UseDepthStencil, RPErrorType.MissingUsageFlag, depthStencil.ToString());
                    return;
                }
            }

            _usedRenderTargets.Add(new UsedRenderTargetData(FGRenderTargetType.DepthStencil, depthStencil));
        }

        public void SetRenderFunction<T>(Action<RasterPassContext, T> function) where T : class, IPassData, new()
        {
            if (_passDataType != typeof(T))
            {
                _renderPass.ReportError(RPErrorSource.SetRenderFunction, RPErrorType.PassDataTypeMismatch, null);
                return;
            }
  
            _function = (x, y) => function(Unsafe.As<RasterPassContext>(x), Unsafe.As<T>(y));
        }

        public void AllowPassCulling(bool allow)
        {
            _allowCulling = allow;
        }

        private static FGTextureUsage[] s_textureUsageMap = [
            FGTextureUsage.PixelShader | FGTextureUsage.RenderTarget | FGTextureUsage.DepthStencil,    //GenericShader
            FGTextureUsage.GenericShader | FGTextureUsage.RenderTarget | FGTextureUsage.DepthStencil,  //PixelShader
            FGTextureUsage.RenderTarget | FGTextureUsage.GenericShader | FGTextureUsage.PixelShader,   //RenderTarget
            FGTextureUsage.DepthStencil | FGTextureUsage.GenericShader | FGTextureUsage.PixelShader,   //DepthStencil
            FGTextureUsage.ShaderResource                                                          ,   //ShaderResource
            ];

        private static FGBufferUsage[] s_bufferUsageMap = [
            FGBufferUsage.ConstantBuffer | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.VertexBuffer,    //ConstantBuffer
            FGBufferUsage.GenericShader | FGBufferUsage.ConstantBuffer | FGBufferUsage.PixelShader | FGBufferUsage.VertexBuffer,    //GenericShader
            FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer | FGBufferUsage.GenericShader | FGBufferUsage.VertexBuffer,    //PixelShader

            FGBufferUsage.VertexBuffer | FGBufferUsage.ConstantBuffer | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader,    //VertexBuffer
            FGBufferUsage.IndexBuffer,                                                                                              //IndexBuffer
            
            FGBufferUsage.Structured | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader,                                      //Structured
            FGBufferUsage.Raw | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader                                              //Raw
            ];
    }

    public enum FGResourceUsage : byte
    {
        Read = 1 << 0,
        Write = 1 << 1,

        NoShaderAccess = 1 << 7,

        ReadWrite = Read | Write
    }

    public record struct UsedResourceData(FGResourceUsage Usage, FrameGraphResource Resource);
    public record struct UsedRenderTargetData(FGRenderTargetType Type, FrameGraphTexture Target);

    public enum FGRenderTargetType : byte
    {
        RenderTarget = 0,
        DepthStencil
    }
}
