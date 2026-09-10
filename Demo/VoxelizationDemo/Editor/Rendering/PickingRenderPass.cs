using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Primary.Assets;
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
            _singleReadSize = (int)RHIFormatInfo.Query(RHIFormat.RGB32_Float).CalculateSize(32, 32);

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
                if (_isWaitingOnRead && _readbackTexture.IsDataReady)
                {
                    _readbackTexture.Read(picking.ReadbackSpan);
                    return;
                }

                RenderPathBlackboard? renderPathBlackboard = renderPass.Blackboard.Get<RenderPathBlackboard>()!;
                if (renderPathBlackboard == null || renderPathBlackboard.RenderList == null)
                    return;

                Int2 pickingResolution = picking.Resolution;
                using (RasterPassDescription desc = renderPass.SetupRasterPass("Picking", out PassData passData))
                {
                    passData.OutDepth = desc.CreateTexture(new FrameGraphTextureDesc
                    {
                        Width = pickingResolution.X,
                        Height = pickingResolution.Y,
                        Dimension = FGTextureDimension._2D,
                        Format = RHIFormat.D24_UNorm_S8_UInt,
                        Usage = FGTextureUsage.DepthStencil
                    }, "Pick Depth");

                    passData.OutPosition = desc.CreateTexture(new FrameGraphTextureDesc
                    {
                        Width = pickingResolution.X,
                        Height = pickingResolution.Y,
                        Dimension = FGTextureDimension._2D,
                        Format = RHIFormat.RGB32_Float,
                        Usage = FGTextureUsage.RenderTarget
                    }, "Pick Position");

                    passData.OutNormal = desc.CreateTexture(new FrameGraphTextureDesc
                    {
                        Width = pickingResolution.X,
                        Height = pickingResolution.Y,
                        Dimension = FGTextureDimension._2D,
                        Format = RHIFormat.RGB32_Float,
                        Usage = FGTextureUsage.RenderTarget
                    }, "Pick Normal");

                    passData.RenderList = renderPathBlackboard.RenderList;
                    passData.Scissor = picking.FocusArea;
                    passData.Readback = _readbackTexture;
                    passData.NormalByteOffset = _singleReadSize;
                    passData.Shader = _pickingShader;
                    passData.Block = _pickingShaderBlock;

                    desc.UseDepthStencil(passData.OutDepth);
                    desc.UseRenderTarget(passData.OutPosition);
                    desc.UseRenderTarget(passData.OutNormal);

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

            cmd.SetRenderTarget(0, passData.OutPosition);
            cmd.SetRenderTarget(1, passData.OutNormal);

            cmd.SetScissor(0, passData.Scissor);

            cmd.SetPipeline(passData.Shader!);
            cmd.SetProperties(passData.Block!);

            MaterialAsset? currentMaterial = null;
            IRenderMeshSource? currentMeshSource = null;

            foreach (ShaderRenderBatcher renderBatcher in passData.RenderList!.ShaderBatchers)
            {
                cmd.SetPipeline(renderBatcher.ActiveShader!);

                foreach (RenderSegment segment in renderBatcher.Segments)
                {
                    if (!segment.Material.IsLoaded)
                        continue;

                    if (currentMaterial != segment.Material)
                    {
                        currentMaterial = segment.Material;
                        cmd.SetProperties(segment.Material.PropertyBlock);
                    }

                    if (currentMeshSource != segment.Mesh.MeshSource)
                    {
                        currentMeshSource = segment.Mesh.MeshSource;
                        if (currentMeshSource == null)
                            continue;

                        cmd.SetVertexBuffer((FrameGraphBuffer)currentMeshSource.VertexBuffer!);
                        cmd.SetIndexBuffer((FrameGraphBuffer)currentMeshSource.IndexBuffer!);
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

            cmd.Read(new FGReadTextureDesc(positionSource, sourceBox, passData.Readback!, 0, 0, 0, 0));
            cmd.Read(new FGReadTextureDesc(normalSource, sourceBox, passData.Readback!, (uint)passData.NormalByteOffset, 0, 0, 0));
        }

        private sealed class PassData : IPassData
        {
            public RenderList? RenderList;

            public FrameGraphTexture OutDepth;
            public FrameGraphTexture OutPosition;
            public FrameGraphTexture OutNormal;

            public FGRect Scissor;

            public RHIReadback? Readback;
            public int NormalByteOffset;

            public ShaderAsset? Shader;
            public PropertyBlock? Block;

            public void Clear()
            {
                throw new NotImplementedException();
            }
        }
    }
}
