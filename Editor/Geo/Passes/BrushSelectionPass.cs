using CommunityToolkit.HighPerformance;
using Editor.Assets.Types;
using Editor.Geo.Selection;
using Editor.Geometry;
using Editor.Geometry.Mesh;
using Editor.Interaction;
using Editor.UI;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Geo.Passes
{
    internal sealed class BrushSelectionPass : IRenderPass
    {
        private ShaderAsset _outlineWrite;
        private ShaderAsset _outlineSample;
        private ShaderAsset _brushVertex;

        private PropertyBlock _writeProperties;
        private PropertyBlock _outlineProperties;
        private PropertyBlock _vertexProperties;

        private int[] _indexMap;

        public BrushSelectionPass()
        {
            _outlineWrite = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Geo/SelectedBrush.shader");
            _outlineSample = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Selection/OutlineSample.shader");
            _brushVertex = AssetManager.LoadAsset<ShaderAsset>("Editor/Shaders/Geo/BrushVertex.shader");

            _writeProperties = _outlineWrite.CreatePropertyBlock();
            _outlineProperties = _outlineSample.CreatePropertyBlock();
            _vertexProperties = _brushVertex.CreatePropertyBlock();

            _indexMap = new int[8];
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            GeoSceneManager geoSceneManager = EditorRuntime.GlobalSingleton.GeoSceneManager;
            GeoSelectionGroup group = geoSceneManager.SelectionGroup;

            if (group.IsEmpty || geoSceneManager.ToolsSnippet == null)
                return;

            RenderCameraData cameraData = context.Get<RenderCameraData>()!;

            if (geoSceneManager.ToolsSnippet.IsAnyEditActive)
            {
                using (RasterPassDescription desc = renderPass.SetupRasterPass("SelEditBrush", out PassData passData))
                {
                    int requiredDynamicVertexCount = 0;
                    int requiredDynamicIndexCount = 0;

                    int totalBrushVertexCount = 0;

                    foreach (var (brush, data) in group.Selection)
                    {
                        //BrushMesh mesh = brush.Group.Scene.Generator.MeshCache.GetMesh(brush);
                        passData.Meshes.Add((brush, data.ActiveData.VertexVertexData, data.FaceData));

                        if (data.IsBrushActive)
                        {
                            requiredDynamicIndexCount += 36;
                            requiredDynamicVertexCount += 8;
                        }
                        else if (data.FaceData > 0)
                        {
                            requiredDynamicIndexCount += int.PopCount(data.FaceData) * 6;
                            requiredDynamicVertexCount += int.PopCount(data.VertexData);
                        }

                        totalBrushVertexCount += 8;
                    }

                    if (passData.Meshes.Count == 0)
                        return;

                    {
                        passData.OutColor = cameraData.ColorTexture;

                        passData.OutlineStencil = desc.CreateTexture(new FrameGraphTextureDesc()
                        {
                            Width = cameraData.ClientSize.X,
                            Height = cameraData.ClientSize.Y,
                            Format = RHIFormat.D24_UNorm_S8_UInt,
                            Usage = FGTextureUsage.PixelShader | FGTextureUsage.ShaderResource | FGTextureUsage.DepthStencil
                        }, "SelGeoStencil");

                        passData.OutlineWrite = _outlineWrite;
                        passData.OutlineSample = _outlineSample;
                        passData.BrushVertex = _brushVertex;

                        passData.OutlineProperties = _outlineProperties;
                        passData.WriteProperties = _writeProperties;
                        passData.VertexProperties = _vertexProperties;

                        desc.UseRenderTarget(passData.OutColor);
                        desc.UseDepthStencil(passData.OutlineStencil);
                    }

                    if ((geoSceneManager.ToolsSnippet.IsEditBrushActive || geoSceneManager.ToolsSnippet.IsEditFaceActive) && requiredDynamicVertexCount > 0)
                    {
                        passData.DynamicVertexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                        {
                            Width = (uint)(Unsafe.SizeOf<Vector3>() * requiredDynamicVertexCount),
                            Stride = Unsafe.SizeOf<Vector3>(),
                            Usage = FGBufferUsage.GenericShader | FGBufferUsage.VertexBuffer
                        }, "SelGeoVertexBuffer");

                        passData.DynamicIndexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                        {
                            Width = (uint)(Unsafe.SizeOf<ushort>() * requiredDynamicIndexCount),
                            Stride = Unsafe.SizeOf<ushort>(),
                            Usage = FGBufferUsage.GenericShader | FGBufferUsage.IndexBuffer
                        }, "SelGeoIndexBuffer");

                        passData.IndexMap = _indexMap;

                        desc.UseResource(FGResourceUsage.ReadWrite, passData.DynamicVertexBuffer);
                        desc.UseResource(FGResourceUsage.ReadWrite, passData.DynamicIndexBuffer);
                    }

                    if (geoSceneManager.ToolsSnippet.IsEditVertexActive && totalBrushVertexCount > 0)
                    {
                        passData.BrushVertexBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                        {
                            Width = (uint)(Unsafe.SizeOf<BrushVertexInstanceData>() * totalBrushVertexCount),
                            Stride = Unsafe.SizeOf<BrushVertexInstanceData>(),
                            Usage = FGBufferUsage.GenericShader | FGBufferUsage.Structured
                        }, "SelGeoBrushVtxBuffer");

                        passData.OutDepth = cameraData.DepthTexture;
                        
                        desc.UseResource(FGResourceUsage.ReadWrite, passData.BrushVertexBuffer);
                        desc.UseResource(FGResourceUsage.Read, cameraData.DepthTexture);
                    }

                    desc.SetRenderFunction<PassData>(static (x, y) => PassFunction(x, y));
                }
            }
        }

        private static void PassFunction(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            if (!passData.BrushVertexBuffer.IsNull)
            {
                int instanceCount = 0;

                // upload data
                {
                    using FGMappedSubresource<BrushVertexInstanceData> instancesMap = cmd.Map<BrushVertexInstanceData>(passData.BrushVertexBuffer);

                    Span<BrushVertexInstanceData> instances = instancesMap.Span;

                    foreach (var (brush, vertices, _) in passData.Meshes)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            instances[instanceCount++] = new BrushVertexInstanceData(brush.Vertices[i], Flags.HasFlag(vertices, 1 << i) ? 1u : 0);
                        }
                    }
                }

                // draw instances
                {
                    cmd.SetRenderTarget(0, passData.OutColor);

                    passData.VertexProperties!.SetResource("sbWorldData", passData.BrushVertexBuffer);
                    passData.VertexProperties!.SetResource("txDepth", passData.OutDepth);

                    cmd.SetPipeline(passData.BrushVertex!);
                    cmd.SetProperties(passData.VertexProperties!);

                    cmd.DrawInstanced(new FGDrawInstancedDesc(6, (uint)instanceCount));
                }
            }

            if (!passData.DynamicVertexBuffer.IsNull && !passData.DynamicIndexBuffer.IsNull)
            {
                int idxCount;

                // upload data
                {
                    using FGMappedSubresource<Vector3> positionsMap = cmd.Map<Vector3>(passData.DynamicVertexBuffer);
                    using FGMappedSubresource<ushort> indicesMap = cmd.Map<ushort>(passData.DynamicIndexBuffer);

                    Span<Vector3> positions = positionsMap.Span;
                    Span<ushort> indices = indicesMap.Span;

                    int vtxOffset = 0;
                    int idxOffset = 0;

                    foreach (var (brush, _, faces) in passData.Meshes)
                    {
                        if (faces > 0)
                        {
                            Array.Fill(passData.IndexMap!, -1);

                            for (int i = 0; i < 6; i++)
                            {
                                if (!Flags.HasFlag(faces, 1 << i))
                                    continue;

                                ImmutableArray<int> indicesData = BrushMeshGenerator.BrushFaceTriangles[i];
                                for (int j = 0; j < indicesData.Length; j++)
                                {
                                    int idxData = indicesData[j];
                                    int idx = passData.IndexMap![idxData];

                                    if (idx == -1)
                                    {
                                        idx = vtxOffset;

                                        positions[vtxOffset++] = brush.Vertices[idxData];
                                        passData.IndexMap[idxData] = idx;
                                    }

                                    indices[idxOffset++] = (ushort)idx;
                                }
                            }
                        }
                    }

                    idxCount = idxOffset;
                }

                // draw outline
                {
                    cmd.ClearDepthStencil(passData.OutlineStencil, FGClearFlags.Stencil, stencil: 0);

                    cmd.SetDepthStencil(passData.OutlineStencil);

                    cmd.SetVertexBuffer(passData.DynamicVertexBuffer);
                    cmd.SetIndexBuffer(passData.DynamicIndexBuffer);

                    cmd.SetPipeline(passData.OutlineWrite!);
                    cmd.SetProperties(passData.WriteProperties!);
                    cmd.SetStencilReference(0xff);

                    cmd.DrawIndexedInstanced(new FGDrawIndexedInstancedDesc((uint)idxCount));
                }

                // sample depth
                {
                    cmd.SetRenderTarget(0, passData.OutColor);
                    cmd.SetDepthStencil(FrameGraphTexture.Invalid);

                    passData.OutlineProperties!.SetResource("txStencil", passData.OutlineStencil, PropertyBindIntent.AsStencil);

                    cmd.SetPipeline(passData.OutlineSample!);
                    cmd.SetProperties(passData.OutlineProperties!);

                    cmd.DrawInstanced(new FGDrawInstancedDesc(3));
                }
            }
        }

        private sealed class PassData : IPassData
        {
            public FrameGraphTexture OutColor;
            public FrameGraphTexture OutDepth;

            public FrameGraphTexture OutlineStencil;

            public ShaderAsset? OutlineWrite;
            public ShaderAsset? OutlineSample;
            public ShaderAsset? BrushVertex;

            public PropertyBlock? WriteProperties;
            public PropertyBlock? OutlineProperties;
            public PropertyBlock? VertexProperties;

            public FrameGraphBuffer DynamicVertexBuffer;
            public FrameGraphBuffer DynamicIndexBuffer;

            public FrameGraphBuffer BrushVertexBuffer;

            public List<(Brush Brush, byte ActiveVertices, byte ActiveFaces)> Meshes = [];

            public int[]? IndexMap;

            public void Clear()
            {
                OutColor = FrameGraphTexture.Invalid;
                OutDepth = FrameGraphTexture.Invalid;

                OutlineStencil = FrameGraphTexture.Invalid;

                OutlineWrite = null;
                OutlineSample = null;
                BrushVertex = null;

                WriteProperties = null;
                OutlineProperties = null;
                VertexProperties = null;

                DynamicVertexBuffer = FrameGraphBuffer.Invalid;
                DynamicIndexBuffer = FrameGraphBuffer.Invalid;

                BrushVertexBuffer = FrameGraphBuffer.Invalid;

                Meshes.Clear();

                IndexMap = null;
            }
        }

        private readonly record struct BrushVertexInstanceData(Vector3 World, uint Active);
    }
}
