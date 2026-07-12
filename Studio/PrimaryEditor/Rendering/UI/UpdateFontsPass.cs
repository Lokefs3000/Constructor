using System;
using System.Collections.Generic;
using System.Text;
using EditorUI;
using EditorUI.Text;
using Primary.Collections.ReadOnly;
using Primary.Rendering;
using Primary.Rendering.Commands;
using Primary.Rendering.Recording;
using Primary.Rendering.Structures;

namespace PrimaryEditor.Rendering.UI
{
    [RenderPassSetup(RunContext = RenderPassRunContext.PerWindow)]
    internal sealed class UpdateFontsPass : IRenderPass
    {
        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            FontRenderer fontRenderer = UIManager.Instance.FontRenderer;
            if (fontRenderer.AtlasUpdates.Count > 0 || fontRenderer.HasGlyphBitmapUpdates)
            {
                using (RasterPassDescription desc = renderPass.SetupRasterPass<GenericPassData>("UpdateFonts", out _))
                {
                    desc.SetRenderFunction<GenericPassData>(PassFunction);
                }
            }
        }

        private static void PassFunction(RasterPassContext context, GenericPassData _)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            FontRenderer fontRenderer = UIManager.Instance.FontRenderer;
            foreach (FontAtlasUpdate atlasUpdate in fontRenderer.AtlasUpdates)
            {
                FontTextureFactory.FontTexture current = (FontTextureFactory.FontTexture)atlasUpdate.CurrentTexture;

                if (atlasUpdate.OldTexture != null)
                {
                    FontTextureFactory.FontTexture src = (FontTextureFactory.FontTexture)atlasUpdate.OldTexture;
                    cmd.Copy(new FGTextureCopyDesc(new FGTextureCopySource(src.RawTexture, 0), null, new FGTextureCopySource(current.RawTexture, 0), 0, 0, 0));
                }
            }

            if (fontRenderer.HasGlyphBitmapUpdates)
            {
                using var lockScope = fontRenderer.TryEnterBitmapLockScope(out bool success, out ROList<GlyphBitmap> bitmaps);
                if (success)
                {
                    foreach (GlyphBitmap bitmap in bitmaps)
                    {
                        FontTextureFactory.FontTexture? texture = (FontTextureFactory.FontTexture?)bitmap.GlyphAtlas.FontTexture;
                        if (texture != null)
                            cmd.Upload(new FGTextureUploadDesc(texture.RawTexture, new FGBox(bitmap.AtlasRect.X, bitmap.AtlasRect.Y, 0, bitmap.AtlasRect.Width, bitmap.AtlasRect.Height, 1), 0, bitmap.AtlasRect.Width * 4), bitmap.PixelsRGBA);
                    }

                    fontRenderer.ClearBitmapsWhenFinished();
                }
            }
        }
    }
}
