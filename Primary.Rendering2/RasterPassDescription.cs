using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Mathematics;
using Primary.Rendering2.Pass;
using Primary.Rendering2.Recording;
using Primary.Rendering2.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2
{
    public ref struct RasterPassDescription : IDisposable
    {
        private readonly RenderPass _renderPass;
        private readonly string _name;

        private PooledList<UsedResourceData> _usedResources;
        private PooledList<UsedRenderTargetData> _usedRenderTargets;

        internal RasterPassDescription(RenderPass renderPass, string name)
        {
            _renderPass = renderPass;
            _name = name;

            _usedResources = new PooledList<UsedResourceData>();
            _usedRenderTargets = new PooledList<UsedRenderTargetData>();
        }

        public void Dispose()
        {
            _renderPass.AddNewRenderPass(new RenderPassDescription(_name, RenderPassType.Graphics, _usedResources, _usedRenderTargets));
        }

        public FrameGraphTexture CreateTexture(FrameGraphTextureDesc desc)
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
                            _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.IncompatibleUsage);
                            return FrameGraphTexture.Invalid;
                        }

                        switch (usage)
                        {
                            case FGTextureUsage.GenericShader:
                            case FGTextureUsage.PixelShader:
                                {
                                    if (!desc.Format.IsTextureFormat(desc.Dimension))
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.IncompatibleFormat);
                                        return FrameGraphTexture.Invalid;
                                    }

                                    break;
                                }
                            case FGTextureUsage.RenderTarget:
                                {
                                    if (desc.Dimension != FGTextureDimension._2D)
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidDimension);
                                        return FrameGraphTexture.Invalid;
                                    }

                                    if (desc.Depth != 1)
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize);
                                        return FrameGraphTexture.Invalid;
                                    }

                                    if (!desc.Format.IsRenderFormat())
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.IncompatibleFormat);
                                        return FrameGraphTexture.Invalid;
                                    }

                                    break;
                                }
                            case FGTextureUsage.DepthStencil:
                                {
                                    if (desc.Dimension != FGTextureDimension._2D)
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidDimension);
                                        return FrameGraphTexture.Invalid;
                                    }

                                    if (desc.Depth != 1)
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize);
                                        return FrameGraphTexture.Invalid;
                                    }

                                    if (!desc.Format.IsDepthFormat())
                                    {
                                        _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.IncompatibleFormat);
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
                                _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize);
                                return FrameGraphTexture.Invalid;
                            }
                            break;
                        }
                    case FGTextureDimension._2D:
                        {
                            if (!IsWithinRange(desc.Width) || !IsWithinRange(desc.Height) || !IsWithinRangeLayers(desc.Depth))
                            {
                                _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize);
                                return FrameGraphTexture.Invalid;
                            }
                            break;
                        }
                    case FGTextureDimension._3D:
                        {
                            if (!IsWithinRange3D(desc.Width) || !IsWithinRange3D(desc.Height) || !IsWithinRange3D(desc.Depth))
                            {
                                _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidSize);
                                return FrameGraphTexture.Invalid;
                            }
                            break;
                        }
                    default:
                        {
                            _renderPass.ReportError(RPErrorSource.CreateTexture, RPErrorType.InvalidDimension);
                            return FrameGraphTexture.Invalid;
                        }
                }

                static bool IsWithinRange(int dim) => (uint)dim > 16384;
                static bool IsWithinRange3D(int dim) => (uint)dim > 2048;
                static bool IsWithinRangeLayers(int dim) => (uint)dim > 2048;
            }

            return new FrameGraphResource(_renderPass.GetNewResourceIndex(), desc).AsTexture();
        }

        public FrameGraphBuffer CreateBuffer(FrameGraphBufferDesc desc)
        {
            //validate
            {
                if (desc.Width == 0)
                {
                    _renderPass.ReportError(RPErrorSource.CreateBuffer, RPErrorType.InvalidSize);
                    return FrameGraphBuffer.Invalid;
                }

                if (desc.Stride > desc.Width)
                {
                    _renderPass.ReportError(RPErrorSource.CreateBuffer, RPErrorType.StrideTooLarge);
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
                            _renderPass.ReportError(RPErrorSource.CreateBuffer, RPErrorType.IncompatibleUsage);
                            return FrameGraphBuffer.Invalid;
                        }
                    }
                }
            }

            return new FrameGraphResource(_renderPass.GetNewResourceIndex(), desc).AsBuffer();
        }

        public void UseResource(FGResourceUsage usage, FrameGraphTexture resource)
        {
            //validate
            {
                switch (usage)
                {
                    case FGResourceUsage.Read:
                        {
                            if (!FlagUtility.HasEither(resource.Description.Usage, FGTextureUsage.GenericShader | FGTextureUsage.PixelShader))
                            {
                                _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.NoShaderAccess);
                            }

                            break;
                        }
                    case FGResourceUsage.Write:
                        {
                            if (!FlagUtility.HasEither(resource.Description.Usage, FGTextureUsage.GenericShader | FGTextureUsage.PixelShader))
                            {
                                _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.NoShaderAccess);
                            }

                            if (!FlagUtility.HasEither(resource.Description.Usage, FGTextureUsage.RenderTarget | FGTextureUsage.DepthStencil))
                            {
                                _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.InvalidUsage);
                            }

                            break;
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
                                _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.NoShaderAccess);
                            }

                            break;
                        }
                    case FGResourceUsage.Write:
                        {
                            if (!FlagUtility.HasEither(resource.Description.Usage, FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.VertexBuffer | FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer))
                            {
                                _renderPass.ReportError(RPErrorSource.UseResource, RPErrorType.NoShaderAccess);
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
                if (!renderTarget.Description.Format.IsRenderFormat())
                {
                    _renderPass.ReportError(RPErrorSource.UseRenderTarget, RPErrorType.IncompatibleFormat);
                    return;
                }

                if (!FlagUtility.HasFlag(renderTarget.Description.Usage, FGTextureUsage.RenderTarget))
                {
                    _renderPass.ReportError(RPErrorSource.UseRenderTarget, RPErrorType.MissingUsageFlag);
                    return;
                }
            }

            _usedRenderTargets.Add(new UsedRenderTargetData(FGRenderTargetType.DepthStencil, renderTarget));
        }

        public void UseDepthStencil(FrameGraphTexture depthStencil)
        {
            //validate
            {
                if (!depthStencil.Description.Format.IsDepthFormat())
                {
                    _renderPass.ReportError(RPErrorSource.UseDepthStencil, RPErrorType.IncompatibleFormat);
                    return;
                }

                if (!FlagUtility.HasFlag(depthStencil.Description.Usage, FGTextureUsage.DepthStencil))
                {
                    _renderPass.ReportError(RPErrorSource.UseDepthStencil, RPErrorType.MissingUsageFlag);
                    return;
                }
            }

            _usedRenderTargets.Add(new UsedRenderTargetData(FGRenderTargetType.DepthStencil, depthStencil));
        }

        public void SetRenderFunction<T>(Action<RasterPassContext, T> function) where T : class, IPassData, new()
        {

        }

        private static FGTextureUsage[] s_textureUsageMap = [
            FGTextureUsage.PixelShader | FGTextureUsage.RenderTarget | FGTextureUsage.DepthStencil,    //GenericShader
            FGTextureUsage.GenericShader | FGTextureUsage.RenderTarget | FGTextureUsage.DepthStencil,  //PixelShader
            FGTextureUsage.RenderTarget | FGTextureUsage.GenericShader | FGTextureUsage.PixelShader,   //RenderTarget
            FGTextureUsage.DepthStencil | FGTextureUsage.GenericShader | FGTextureUsage.PixelShader,   //DepthStencil
            ];

        private static FGBufferUsage[] s_bufferUsageMap = [
            FGBufferUsage.ConstantBuffer | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader | FGBufferUsage.VertexBuffer,    //ConstantBuffer
            FGBufferUsage.GenericShader | FGBufferUsage.ConstantBuffer | FGBufferUsage.PixelShader | FGBufferUsage.VertexBuffer,    //GenericShader
            FGBufferUsage.PixelShader | FGBufferUsage.ConstantBuffer | FGBufferUsage.GenericShader | FGBufferUsage.VertexBuffer,    //PixelShader

            FGBufferUsage.VertexBuffer | FGBufferUsage.ConstantBuffer | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader,    //VertexBuffer
            FGBufferUsage.IndexBuffer,                                                                                              //IndexBuffer
            
            FGBufferUsage.Structured | FGBufferUsage.GenericShader | FGBufferUsage.PixelShader                                      //Structured
            ];
    }

    public enum FGResourceUsage : byte
    {
        Read = 1 << 0,
        Write = 1 << 1,
    }

    internal record struct UsedResourceData(FGResourceUsage Usage, FrameGraphResource Resource);
    internal record struct UsedRenderTargetData(FGRenderTargetType Type, FrameGraphTexture Target);

    internal enum FGRenderTargetType : byte
    {
        RenderTarget = 0,
        DepthStencil
    }
}
