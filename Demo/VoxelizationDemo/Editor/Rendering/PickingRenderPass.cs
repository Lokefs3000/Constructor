using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Assets;
using Primary.Common;
using Primary.Mathematics;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Batching;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;
using VoxelizationDemo.Rendering;

namespace VoxelizationDemo.Editor.Rendering
{
    internal sealed class PickingRenderPass : IRenderPass, IDisposable
    {
        private readonly int _singleReadSize;
        private readonly RHIReadback _readbackTexture;

        private bool _isWaitingOnRead;

        private readonly ShaderAsset _pickingShader;
        private readonly PropertyBlock _pickingShaderBlock;

        public PickingRenderPass()
        {
            _singleReadSize = (int)RHIFormatInfo.Query(RHIFormat.RGBA16_Float).CalculateSize(32, 32);

            _readbackTexture = RHIDevice.Instance!.CreateReadback(new RHIReadbackDescription
            {
                Width = (uint)(_singleReadSize * 2)
            })!;

            _pickingShader = AssetManager.LoadAsset<ShaderAsset>("Content/Shaders/Editor/PickingWrite.shader");
            _pickingShaderBlock = _pickingShader.CreatePropertyBlock();
        }

        public void Dispose()
        {
            _readbackTexture.Dispose();
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            if (!_pickingShader.IsLoaded)
                return;

            PickingManager picking = EditorManager.Instance.PickingManager;
            if (picking.WantsNewPickingData)
            {
                if (_isWaitingOnRead)
                {
                    if (_readbackTexture.IsDataReady)
                    {
                        _readbackTexture.Read(picking.ReadbackSpan);
                        _isWaitingOnRead = false;

                        picking.ConsumePickRequest();
                    }

                    return;
                }

                RenderPathBlackboard? renderPathBlackboard = renderPass.Blackboard.Get<RenderPathBlackboard>()!;
                if (renderPathBlackboard == null || renderPathBlackboard.RenderList == null)
                    return;

                Int2 pickingResolution = picking.Resolution;
                using (RasterPassDescription desc = renderPass.SetupRasterPass("Picking", out PassData passData))
                {
                    passData.OutDepth = desc.CreateTexture(new FGTextureDesc
                    {
                        Width = pickingResolution.X,
                        Height = pickingResolution.Y,
                        Dimension = FGTextureDimension._2D,
                        Format = RHIFormat.D24_UNorm_S8_UInt,
                        Usage = FGTextureUsage.DepthStencil
                    }, "Pick Depth");

                    passData.OutPosition = desc.CreateTexture(new FGTextureDesc
                    {
                        Width = pickingResolution.X,
                        Height = pickingResolution.Y,
                        Dimension = FGTextureDimension._2D,
                        Format = RHIFormat.RGBA16_Float,
                        Usage = FGTextureUsage.RenderTarget | FGTextureUsage.ShaderResource
                    }, "Pick Position");

                    passData.OutNormal = desc.CreateTexture(new FGTextureDesc
                    {
                        Width = pickingResolution.X,
                        Height = pickingResolution.Y,
                        Dimension = FGTextureDimension._2D,
                        Format = RHIFormat.RGBA16_Float,
                        Usage = FGTextureUsage.RenderTarget | FGTextureUsage.ShaderResource
                    }, "Pick Normal");

                    passData.OutCameraColor = FGTexture.Invalid; // context.Get<RenderCameraData>()!.ColorTexture;

                    passData.RenderList = renderPathBlackboard.RenderList;
                    passData.Scissor = picking.FocusArea;
                    passData.Readback = _readbackTexture;
                    passData.NormalByteOffset = _singleReadSize;
                    passData.Shader = _pickingShader;
                    passData.Block = _pickingShaderBlock;

                    desc.UseResource(FGResourceUsage.Read | FGResourceUsage.NoShaderAccess, passData.OutPosition);
                    desc.UseResource(FGResourceUsage.Read | FGResourceUsage.NoShaderAccess, passData.OutNormal);

                    desc.UseDepthStencil(passData.OutDepth);
                    desc.UseRenderTarget(passData.OutPosition);
                    desc.UseRenderTarget(passData.OutNormal);

                    if (!passData.OutCameraColor.IsNull)
                        desc.UseRenderTarget(passData.OutCameraColor);

                    desc.AllowPassCulling(false);
                    desc.SetRenderFunction<PassData>(PassFunction);
                }

                _isWaitingOnRead = true;
            }
        }

        private static void PassFunction(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            cmd.ClearDepthStencil(passData.OutDepth, FGClearFlags.Depth, null);
            cmd.SetDepthStencil(passData.OutDepth);

            cmd.ClearRenderTarget(passData.OutPosition, Color.TransparentBlack);
            cmd.ClearRenderTarget(passData.OutNormal, Color.TransparentBlack);

            cmd.SetRenderTarget(0, passData.OutPosition);
            cmd.SetRenderTarget(1, passData.OutNormal);

            //cmd.SetScissor(0, passData.Scissor);

            cmd.SetPipeline(passData.Shader!);
            cmd.SetProperties(passData.Block!);

            IRenderMeshSource? currentMeshSource = null;

            foreach (ShaderRenderBatcher renderBatcher in passData.RenderList!.ShaderBatchers)
            {
                foreach (RenderSegment segment in renderBatcher.Segments)
                {
                    if (currentMeshSource != segment.Mesh.MeshSource)
                    {
                        currentMeshSource = segment.Mesh.MeshSource;
                        if (currentMeshSource == null)
                            continue;

                        cmd.SetVertexBuffer((FGBuffer)currentMeshSource.VertexBuffer!);
                        cmd.SetIndexBuffer((FGBuffer)currentMeshSource.IndexBuffer!);
                    }
                    else
                    {
                        if (currentMeshSource == null)
                            continue;
                    }

                    cmd.SetConstants((uint)segment.FlagIndexStart);

                    ref readonly RenderMeshDrawArgs drawArgs = ref segment.Mesh.Args;
                    if (drawArgs.NeedsIndexedDraw)
                        cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc(drawArgs.VertexOrIndexCount, (uint)(segment.FlagIndexEnd - segment.FlagIndexStart), drawArgs.IndexOffset, (int)drawArgs.VertexOffset));
                    else
                        cmd.DrawInstanced(new FGDrawInstancedDesc(drawArgs.VertexOrIndexCount, (uint)(segment.FlagIndexEnd - segment.FlagIndexStart), drawArgs.VertexOffset));
                }
            }

            FGBox sourceBox = new FGBox(passData.Scissor.Left, passData.Scissor.Top, 0, passData.Scissor.Width, passData.Scissor.Height, 1);

            FGTextureCopySource positionSource = new FGTextureCopySource(passData.OutPosition, 0);
            FGTextureCopySource normalSource = new FGTextureCopySource(passData.OutNormal, 0);

            FGTextureFootprint positionFootprint = new FGTextureFootprint(0, RHIFormat.RGBA16_Float, (uint)sourceBox.Width, (uint)sourceBox.Height, 1, (uint)RHIFormatInfo.Query(RHIFormat.RGBA16_Float).CalculatePitch(sourceBox.Width));
            FGTextureFootprint normalFootprint = new FGTextureFootprint((uint)passData.NormalByteOffset, positionFootprint.Format, positionFootprint.Width, positionFootprint.Height, positionFootprint.Depth, positionFootprint.RowPitch);

            cmd.Read(new FGReadTextureDesc(positionSource, sourceBox, passData.Readback!, positionFootprint, 0, 0, 0));
            cmd.Read(new FGReadTextureDesc(normalSource, sourceBox, passData.Readback!, normalFootprint, 0, 0, 0));

            if (!passData.OutCameraColor.IsNull)
            {
                cmd.SetRenderTarget(0, passData.OutCameraColor);
                cmd.SetRenderTarget(1, FGTexture.Invalid);

                Blitter.Blit(in cmd, passData.OutNormal);
            }
        }

        private sealed class PassData : IPassData
        {
            public RenderList? RenderList;

            public FGTexture OutDepth;
            public FGTexture OutPosition;
            public FGTexture OutNormal;

            public FGTexture OutCameraColor;

            public FGRect Scissor;

            public RHIReadback? Readback;
            public int NormalByteOffset;

            public ShaderAsset? Shader;
            public PropertyBlock? Block;

            public void Clear()
            {
                RenderList = null;

                OutDepth = FGTexture.Invalid;
                OutPosition = FGTexture.Invalid;
                OutNormal = FGTexture.Invalid;

                OutCameraColor = FGTexture.Invalid;

                Scissor = default;

                Readback = null;
                NormalByteOffset = 0;

                Shader = null;
                Block = null;
            }
        }
    }
}
