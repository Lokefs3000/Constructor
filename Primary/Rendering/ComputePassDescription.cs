using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Common;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.RHI;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Primary.Rendering
{
    public ref struct ComputePassDescription : IDisposable
    {
        private readonly RenderPass _renderPass;
        private readonly string _name;
        private readonly IPassData _passData;

        private PassArray _array;

        private Action<object, IPassContext, IPassData>? _function;
        private object? _realFunction;

        private bool _allowCulling;

        internal ComputePassDescription(RenderPass renderPass, string name, IPassData passData, PassArray array)
        {
            _renderPass = renderPass;
            _name = name;
            _passData = passData;

            _array = array;

            _function = null;
            _realFunction = null;

            _allowCulling = true;
        }

        public void Dispose()
        {
            if (_function != null)
            {
                RenderPass.AddGlobalResources(ref _array);
                _array.Commit();
                _renderPass.AddNewRenderPass(new RenderPassDescription(_name, _renderPass.CurrentGroupIndex, RenderPassType.Compute, _array.UsedResources, _array.UsedRenderTargets, _passData, _function, _realFunction, _allowCulling));
            }
            else
            {
                _array.Commit();
            }
        }

        public FGTexture CreateTexture(FGTextureDesc desc, [CallerMemberName] string? debugName = null)
        {
            //validate
            {
                int start = 31 - int.LeadingZeroCount((int)desc.Usage);
                for (int i = start; i >= 0; i--)
                {
                    FGTextureUsage usage = (FGTextureUsage)(1 << i);
                    if (Flags.HasFlag(desc.Usage, usage))
                    {
                        if (Flags.HasFlag(desc.Usage, ~s_textureUsageMap.DangerousGetReferenceAt(i)))
                        {
                            _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.IncompatibleUsage, debugName);
                            return FGTexture.Invalid;
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
                                        return FGTexture.Invalid;
                                    }

                                    if (desc.Depth != 1)
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize, debugName);
                                        return FGTexture.Invalid;
                                    }

                                    if (!desc.Format.IsRenderTargetCapable())
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.IncompatibleFormat, debugName);
                                        return FGTexture.Invalid;
                                    }

                                    break;
                                }
                            case FGTextureUsage.DepthStencil:
                                {
                                    if (desc.Dimension != FGTextureDimension._2D)
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidDimension, debugName);
                                        return FGTexture.Invalid;
                                    }

                                    if (desc.Depth != 1)
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize, debugName);
                                        return FGTexture.Invalid;
                                    }

                                    if (!desc.Format.IsDepthStencilCapable())
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.IncompatibleFormat, debugName);
                                        return FGTexture.Invalid;
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
                                return FGTexture.Invalid;
                            }
                            break;
                        }
                    case FGTextureDimension._2D:
                        {
                            if (!IsWithinRange(desc.Width) || !IsWithinRange(desc.Height) || !IsWithinRangeLayers(desc.Depth))
                            {
                                _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize, debugName);
                                return FGTexture.Invalid;
                            }
                            break;
                        }
                    case FGTextureDimension._3D:
                        {
                            if (!IsWithinRange3D(desc.Width) || !IsWithinRange3D(desc.Height) || !IsWithinRange3D(desc.Depth))
                            {
                                _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize, debugName);
                                return FGTexture.Invalid;
                            }
                            break;
                        }
                    default:
                        {
                            _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidDimension, debugName);
                            return FGTexture.Invalid;
                        }
                }

                static bool IsWithinRange(int dim) => (uint)dim <= 16384;
                static bool IsWithinRange3D(int dim) => (uint)dim <= 2048;
                static bool IsWithinRangeLayers(int dim) => (uint)dim <= 2048;
            }

            FGTexture texture = new FGResource(_renderPass.GetNewResourceIndex(), desc, debugName).AsTexture();
            _renderPass.Manager.Resources.AddFGResource(texture);

            return texture;
        }

        public FGBuffer CreateBuffer(FGBufferDesc desc, [CallerMemberName] string? debugName = null)
        {
            //validate
            {
                if (desc.Width == 0)
                {
                    _renderPass.ReportError(RPErrorSource.CreateBuffer, RPErrorType.InvalidSize, debugName);
                    return FGBuffer.Invalid;
                }

                if (desc.Stride > desc.Width)
                {
                    _renderPass.ReportError(RPErrorSource.CreateBuffer, RPErrorType.StrideTooLarge, debugName);
                    return FGBuffer.Invalid;
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
                    if (Flags.HasFlag(desc.Usage, usage))
                    {
                        if (Flags.HasFlag(desc.Usage, ~s_bufferUsageMap.DangerousGetReferenceAt(i)))
                        {
                            _renderPass.ReportError(RPErrorSource.CreateBuffer, RPErrorType.IncompatibleUsage, debugName);
                            return FGBuffer.Invalid;
                        }
                    }
                }
            }

            //Not a bug but a D3D12 limitation
            if (Flags.HasFlag(desc.Usage, FGBufferUsage.ConstantBuffer))
                desc.Width = Math.Max(desc.Width, 256);

            FGBuffer buffer = new FGResource(_renderPass.GetNewResourceIndex(), desc, debugName).AsBuffer();
            _renderPass.Manager.Resources.AddFGResource(buffer);

            return buffer;
        }

        public void UseResource(FGResourceUsage usage, FGTexture resource)
        {
            //validate
            {
                if (Flags.HasFlag(usage, FGResourceUsage.Read))
                {
                    if (!Flags.HasEither(resource.Description.Usage, FGTextureUsage.GenericShader | FGTextureUsage.PixelShader))
                    {
                        if (!Flags.HasFlag(usage, FGResourceUsage.NoShaderAccess))
                            _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.NoShaderAccess, resource.ToString());
                    }
                }

                if (Flags.HasFlag(usage, FGResourceUsage.Write))
                {
                    if (!Flags.HasEither(resource.Description.Usage, FGTextureUsage.GenericShader | FGTextureUsage.PixelShader))
                    {
                        _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.NoShaderAccess, resource.ToString());
                    }

                    if (!Flags.HasEither(resource.Description.Usage, FGTextureUsage.RenderTarget | FGTextureUsage.DepthStencil | FGTextureUsage.UnorderedAccess))
                    {
                        _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.InvalidUsage, resource.ToString());
                    }
                }
            }

            _array.AddResource(new UsedResourceData(usage, resource));
        }

        public void UseResource(FGResourceUsage usage, FGBuffer resource)
        {
            //validate
            {
                switch (usage)
                {
                    case FGResourceUsage.Read:
                        {
                            if (!Flags.HasEither(resource.Description.Usage, FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.VertexBuffer | FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer))
                            {
                                _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.NoShaderAccess, resource.ToString());
                            }

                            break;
                        }
                    case FGResourceUsage.Write:
                        {
                            if (!Flags.HasEither(resource.Description.Usage, FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.VertexBuffer | FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer))
                            {
                                _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.NoShaderAccess, resource.ToString());
                            }

                            break;
                        }
                }
            }

            _array.AddResource(new UsedResourceData(usage, resource));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UseResource(FGResourceUsage usage, FGResource resource)
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

        public void UseRenderTarget(FGTexture renderTarget)
        {
            //validate
            {
                if (!renderTarget.Description.Format.IsRenderTargetCapable())
                {
                    _renderPass.ReportError(RPErrorSource.UseRenderTarget, RPErrorType.IncompatibleFormat, renderTarget.ToString());
                    return;
                }

                if (!Flags.HasFlag(renderTarget.Description.Usage, FGTextureUsage.RenderTarget))
                {
                    _renderPass.ReportError(RPErrorSource.UseRenderTarget, RPErrorType.MissingUsageFlag, renderTarget.ToString());
                    return;
                }
            }

            _array.AddRenderTarget(new UsedRenderTargetData(FGRenderTargetType.RenderTarget, renderTarget));
        }

        public void UseDepthStencil(FGTexture depthStencil)
        {
            //validate
            {
                if (!depthStencil.Description.Format.IsDepthStencilCapable())
                {
                    _renderPass.ReportError(RPErrorSource.UseDepthStencil, RPErrorType.IncompatibleFormat, depthStencil.ToString());
                    return;
                }

                if (!Flags.HasFlag(depthStencil.Description.Usage, FGTextureUsage.DepthStencil))
                {
                    _renderPass.ReportError(RPErrorSource.UseDepthStencil, RPErrorType.MissingUsageFlag, depthStencil.ToString());
                    return;
                }
            }

            _array.AddRenderTarget(new UsedRenderTargetData(FGRenderTargetType.DepthStencil, depthStencil));
        }

        public void SetRenderFunction<T>(Action<ComputePassContext, T> function) where T : class, IPassData, new()
        {
            if (_passData is not T)
            {
                _renderPass.ReportError(RPErrorSource.SetRenderFunction, RPErrorType.PassDataTypeMismatch, null);
                return;
            }

            _function = (f, x, y) => Unsafe.As<Action<ComputePassContext, T>>(f)(Unsafe.As<ComputePassContext>(x), Unsafe.As<T>(y));
            _realFunction = function;
        }

        public void AllowPassCulling(bool allow)
        {
            _allowCulling = allow;
        }

        private static readonly FGTextureUsage[] s_textureUsageMap = [
            FGTextureUsage.PixelShader | FGTextureUsage.RenderTarget | FGTextureUsage.DepthStencil,    //GenericShader
            FGTextureUsage.GenericShader | FGTextureUsage.RenderTarget | FGTextureUsage.DepthStencil,  //PixelShader
            FGTextureUsage.RenderTarget | FGTextureUsage.GenericShader | FGTextureUsage.PixelShader,   //RenderTarget
            FGTextureUsage.DepthStencil | FGTextureUsage.GenericShader | FGTextureUsage.PixelShader,   //DepthStencil
            FGTextureUsage.ShaderResource | FGTextureUsage.UnorderedAccess,                            //ShaderResource
            FGTextureUsage.UnorderedAccess | FGTextureUsage.ShaderResource,                            //UnorderedAccess
            ];

        private static readonly FGBufferUsage[] s_bufferUsageMap = [
            FGBufferUsage.ConstantBuffer | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.VertexBuffer,    //ConstantBuffer
            FGBufferUsage.GenericShader | FGBufferUsage.ConstantBuffer | FGBufferUsage.PixelShader | FGBufferUsage.VertexBuffer,    //GenericShader
            FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer | FGBufferUsage.GenericShader | FGBufferUsage.VertexBuffer,    //PixelShader

            FGBufferUsage.VertexBuffer | FGBufferUsage.ConstantBuffer | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader,    //VertexBuffer
            FGBufferUsage.IndexBuffer,                                                                                              //IndexBuffer
            
            FGBufferUsage.Structured | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader,                                      //Structured
            FGBufferUsage.Raw | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader                                              //Raw
            ];
    }
}
