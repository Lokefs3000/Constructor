using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary.Assets;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Data;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Rendering.Icons;

namespace VoxelizationDemo.Editor.Rendering
{
    internal sealed class DrawWorldIconsPass : IRenderPass
    {
        private readonly ShaderAsset _worldIconsShader;
        private readonly PropertyBlock _worldIconsBlock;

        public DrawWorldIconsPass()
        {
            _worldIconsShader = AssetManager.LoadAsset<ShaderAsset>("Content/Shaders/Editor/WorldIcons.shader");
            _worldIconsBlock = _worldIconsShader.CreatePropertyBlock();
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            WorldIconManager iconManager = VoxelRuntime.Instance.EditorManager.WorldIconManager;
            if (iconManager.TotalIconCount == 0 || !_worldIconsShader.IsLoaded)
                return;

            TextureAsset? texture = iconManager.IconSources[0].Sprite?.Texture;
            if (texture == null)
                return;

            RenderCameraData cameraData = context.Get<RenderCameraData>()!;

            using (RasterPassDescription desc = renderPass.SetupRasterPass("DrawWorldIcons", out PassData passData))
            {
                passData.IconBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)(Unsafe.SizeOf<WorldIconData>() * iconManager.TotalIconCount),
                    Stride = Unsafe.SizeOf<WorldIconData>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.Structured
                }, "IconsBuffer");

                passData.OutColor = cameraData.ColorTexture;
                passData.OutDepth = cameraData.DepthTexture;

                passData.Shader = _worldIconsShader;
                passData.Block = _worldIconsBlock;

                passData.IconManager = iconManager;
                passData.Texture = texture;

                desc.UseResource(FGResourceUsage.ReadWrite, passData.IconBuffer);
                desc.UseRenderTarget(passData.OutColor);
                desc.UseDepthStencil(passData.OutDepth);

                desc.SetRenderFunction<PassData>(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            using (FGMappedSubresource<WorldIconData> mapped = cmd.Map<WorldIconData>(passData.IconBuffer))
            {
                int baseOffset = 0;
                foreach (WorldIconSource iconSource in passData.IconManager!.IconSources)
                {
                    if (iconSource.Icons.Count > 0)
                    {
                        iconSource.Icons.AsSpan().CopyTo(mapped.Span[baseOffset..]);
                        baseOffset += iconSource.Icons.Count;
                    }
                }
            }

            Vector2 scale = Vector2.One / new Vector2(passData.OutColor.Description.Width, passData.OutColor.Description.Height) * 32.0f;

            cmd.SetRenderTarget(0, passData.OutColor);
            cmd.SetDepthStencil(passData.OutDepth);

            cmd.SetPipeline(passData.Shader!);
            cmd.SetProperties(passData.Block!);

            cmd.SetConstants(scale);

            passData.Block!.SetResource("sbIcons", passData.IconBuffer);
            passData.Block!.SetResource("txIconAtlas", passData.Texture!);

            cmd.DrawInstanced(new FGDrawInstancedDesc(6, (uint)passData.IconManager.TotalIconCount));
        }

        private sealed class PassData : IPassData
        {
            public WorldIconManager? IconManager;
            public TextureAsset? Texture;

            public FrameGraphTexture OutColor;
            public FrameGraphTexture OutDepth;

            public FrameGraphBuffer IconBuffer;

            public ShaderAsset? Shader;
            public PropertyBlock? Block;

            public void Clear()
            {
                IconManager = null;

                OutColor = FrameGraphTexture.Invalid;
                OutDepth = FrameGraphTexture.Invalid;

                IconBuffer = FrameGraphBuffer.Invalid;

                Shader = null;
                Block = null;
            }
        }
    }
}
