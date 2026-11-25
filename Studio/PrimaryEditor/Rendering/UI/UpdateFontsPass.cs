using System;
using System.Collections.Generic;
using System.Text;
using EditorUI;
using EditorUI.Text;
using Primary.Collections.ReadOnly;
using Primary.Rendering;
using Primary.Rendering.Recording;
using Primary.Rendering.Commands;
using Primary.Mathematics;

namespace PrimaryEditor.Rendering.UI
{
    internal sealed class UpdateFontsPass : IRenderPass
    {
        public void SetupPasses(FrameGraphBuilder frameGraph)
        {
            FontRenderer fontRenderer = UIManager.Instance.FontRenderer;
            if (fontRenderer.AtlasUpdates.Count > 0 || fontRenderer.HasGlyphBitmapUpdates)
            {
                using (RasterPassBuilder desc = frameGraph.AddRasterPass("UpdateFonts"))
                {
                    desc.SetFunction(PassFunction);
                }
            }
        }

        private static void PassFunction(RasterPassContext context)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;

            FontRenderer fontRenderer = UIManager.Instance.FontRenderer;
            foreach (FontAtlasUpdate atlasUpdate in fontRenderer.AtlasUpdates)
            {
                FontTextureFactory.FontTexture current = (FontTextureFactory.FontTexture)atlasUpdate.CurrentTexture;

                if (atlasUpdate.OldTexture != null)
                {
                    FontTextureFactory.FontTexture src = (FontTextureFactory.FontTexture)atlasUpdate.OldTexture;
                    //cmd.CopyTexture(current.RawTexture, 0, Int3.Zero, src.RawTexture, 0, null);
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
                        //if (texture != null)
                        //    cmd.Upload(new FGTextureUploadDesc(texture.RawTexture, new FGBox(bitmap.AtlasRect.X, bitmap.AtlasRect.Y, 0, bitmap.AtlasRect.Width, bitmap.AtlasRect.Height, 1), 0, bitmap.AtlasRect.Width * 4), bitmap.PixelsRGBA);
                    }

                    fontRenderer.ClearBitmapsWhenFinished();
                }
            }
        }
    }
}
