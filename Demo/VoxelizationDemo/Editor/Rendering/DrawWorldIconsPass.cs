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

        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            WorldIconManager iconManager = VoxelRuntime.Instance.EditorManager.WorldIconManager;
            if (iconManager.TotalIconCount == 0 || !_worldIconsShader.IsLoaded)
                return;

            TextureAsset? texture = iconManager.IconSources[0].Sprite?.Texture;
            if (texture == null)
                return;

            RenderCameraData cameraData = frameGraph.GetContext<RenderCameraData>();

            using (RasterPassBuilder desc = frameGraph.AddRasterPass("DrawWorldIcons", out PassData passData))
            {
                passData.IconBuffer = desc.CreateBuffer(new FGBufferDesc
                {
                    Name = "IconsBuffer",
                    ElementCount = iconManager.TotalIconCount,
                    Stride = Unsafe.SizeOf<WorldIconData>(),
                    Target = FGBufferTarget.Structured
                });

                passData.OutColor = cameraData.ColorTexture;
                passData.OutDepth = cameraData.DepthTexture;

                passData.Shader = _worldIconsShader;
                passData.Block = _worldIconsBlock;

                passData.IconManager = iconManager;
                passData.Texture = texture;

                desc.ReadWrite(passData.IconBuffer);
                desc.Write(passData.OutColor);
                desc.Read(passData.OutDepth);

                desc.SetFunction(PassFunction);
            }
        }

        private static void PassFunction(RasterPassContext context)
        {
            PassData passData = context.GetPassData<PassData>()!;
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

            ref readonly FGTextureDesc outColorDesc = ref context.GetDescription(passData.OutColor);
            Vector2 scale = Vector2.One / new Vector2(outColorDesc.Width, outColorDesc.Height) * 32.0f;

            cmd.SetRenderTarget(0, passData.OutColor);
            cmd.SetDepthStencil(passData.OutDepth);

            cmd.SetShader(passData.Shader!);
            cmd.SetProperties(passData.Block!);

            cmd.SetConstants(scale);

            passData.Block!.SetResource("sbIcons", passData.IconBuffer);
            passData.Block!.SetResource("txIconAtlas", passData.Texture!);

            cmd.DrawInstanced(6, (uint)passData.IconManager.TotalIconCount, 0, 0);
        }

        private sealed class PassData : IPassData
        {
            public WorldIconManager? IconManager;
            public TextureAsset? Texture;

            public TextureHandle OutColor;
            public TextureHandle OutDepth;

            public BufferHandle IconBuffer;

            public ShaderAsset? Shader;
            public PropertyBlock? Block;

            public void Clear()
            {
                IconManager = null;

                OutColor = TextureHandle.Null;
                OutDepth = TextureHandle.Null;

                IconBuffer = BufferHandle.Null;

                Shader = null;
                Block = null;
            }
        }
    }
}
