using Editor.Assets.Types;
using Editor.Geo;
using Editor.Geometry;
using Editor.Geometry.Mesh;
using Primary.Rendering;
using Primary.Rendering.Commands;
using Primary.Rendering.Pass;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;
using Primary.Timing;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Rendering.Passes
{
    public sealed class GeoScenePass : IRenderPass
    {
        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            GeoSceneManager manager = EditorRuntime.GlobalSingleton.GeoSceneManager;
            HashSet<(GeoSceneAsset asset, GeoScene scene)> invalidScenes = manager.InvalidScenes;

            if (invalidScenes.Count > 0)
            {
                lock (manager.Lock)
                {
                    foreach ((GeoSceneAsset asset, GeoScene scene) in invalidScenes)
                    {
                        if (!asset.IsLoaded)
                            return;

                        using (RasterPassDescription desc = renderPass.SetupRasterPass("UpdateGeoScene", out PassData passData))
                        {
                            passData.Asset = asset;
                            passData.Scene = scene;

                            if (false && scene.Container.NewVertexCount > 0)
                            {
                                passData.TransientBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                                {
                                    Width = (uint)(Unsafe.SizeOf<BrushVertex>() * scene.Container.NewVertexCount),
                                    Stride = Unsafe.SizeOf<BrushVertex>(),
                                    Usage = FGBufferUsage.Undefined
                                }, "GeoUploadBuffer");

                                desc.UseResource(FGResourceUsage.ReadWrite, passData.TransientBuffer);
                            }
                            else
                                passData.TransientBuffer = FrameGraphBuffer.Invalid;

                            desc.AllowPassCulling(false);
                            desc.SetRenderFunction<PassData>(static (x, y) => PassFunction(x, y));
                        }

                        asset.ConsumeNewMeshData();
                    }

                    invalidScenes.Clear();
                }
            }
        }

        private static void PassFunction(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            passData.Asset!.ResizeVertexBuffer(out bool isBufferNew);

            RHIBuffer? vertexBuffer = passData.Asset!.VertexBuffer;
            if (vertexBuffer == null)
                return;

            MeshContainer container = passData.Scene!.Container;
            if (isBufferNew || true)
            {
                cmd.Upload(new FGBufferUploadDesc(vertexBuffer, 0), container.Vertices);
            }
            else
            {
                foreach (MeshVertexUpdate vertexUpdate in container.VertexUpdates)
                {
                    cmd.Copy(new FGBufferCopyDesc(
                        vertexBuffer, (uint)(Unsafe.SizeOf<BrushVertex>() * vertexUpdate.SourceIndex),
                        vertexBuffer, (uint)(Unsafe.SizeOf<BrushVertex>() * vertexUpdate.DestinationIndex),
                        (uint)(Unsafe.SizeOf<BrushVertex>() * vertexUpdate.Count)));
                }

                foreach (MeshFaceUpdate faceUpdate in container.FaceUpdates)
                {
                    cmd.Upload(new FGBufferUploadDesc(vertexBuffer, (uint)(faceUpdate.VertexOffset * Unsafe.SizeOf<BrushVertex>())), container.Vertices.Slice(faceUpdate.VertexOffset, 6));
                }

                //if (!passData.TransientBuffer.IsNull)
                //{
                //    int vertexOffset = 0;
                //    {
                //        using FGMappedSubresource<BrushVertex> mapped = cmd.Map<BrushVertex>(passData.TransientBuffer);
                //
                //        foreach (MeshFaceUpdate faceUpdate in container.FaceUpdates)
                //        {
                //            Span<BrushVertex> source = container.Vertices[faceUpdate.VertexOffset..(faceUpdate.VertexOffset + 6)];
                //            Span<BrushVertex> destination = mapped.Span[vertexOffset..(vertexOffset + 6)];
                //
                //            source.CopyTo(destination);
                //
                //            vertexOffset += 6;
                //        }
                //    }
                //
                //    vertexOffset = 0;
                //    foreach (MeshFaceUpdate faceUpdate in container.FaceUpdates)
                //    {
                //        cmd.Copy(new FGBufferCopyDesc(
                //            passData.TransientBuffer, (uint)(Unsafe.SizeOf<BrushVertex>() * vertexOffset),
                //            vertexBuffer, (uint)(Unsafe.SizeOf<BrushVertex>() * faceUpdate.VertexOffset),
                //            (uint)Unsafe.SizeOf<BrushVertex>() * 6));
                //
                //        vertexOffset += 6;
                //    }
                //}
            }
        }

        private sealed class PassData : IPassData
        {
            public GeoSceneAsset? Asset;
            public GeoScene? Scene;

            public FrameGraphBuffer TransientBuffer;

            public void Clear()
            {
                Asset = null;
                Scene = null;

                TransientBuffer = FrameGraphBuffer.Invalid;
            }
        }
    }
}
