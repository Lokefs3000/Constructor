using Primary.Rendering;
using Primary.Rendering.Commands;
using Primary.Rendering.Recording;
using Primary.Rendering.Structures;
using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Visual.Passes
{
    internal sealed class UIUpdateFontsRenderPass : IRenderPass
    {
        public UIUpdateFontsRenderPass()
        {

        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            UIFontManager fontManager = UIManager.Instance.FontManager;
            if (fontManager.DoAnyFontsNeedUpdates)
            {
                using (RasterPassDescription desc = renderPass.SetupRasterPass("UI-UpdFonts", out PassData data))
                {
                    data.FontManager = fontManager;

                    desc.AllowPassCulling(false);
                    desc.SetRenderFunction<PassData>(PassFunction);
                }
            }
        }

        private static void PassFunction(RasterPassContext context, PassData data)
        {
            RasterCommandBuffer cmd = context.CommandBuffer;
            while (data.FontManager!.PendingFontUpdates.TryDequeue(out UIFontUpdate update))
            {
                RHITexture dest = update.Style.AtlasTexture!;
                if (update.LoadIndex == update.Style.Font.LoadIndex && update.OldAtlas != null)
                {
                    cmd.Copy(new FGTextureCopyDesc(
                        new FGTextureCopySource(update.OldAtlas, 0), null,
                        new FGTextureCopySource(dest, 0), 0, 0, 0));
                }

                update.OldAtlas?.Dispose();

                foreach (UIGlyphBitmap bitmap in update.Bitmaps)
                {
                    FGBox box = new FGBox(bitmap.TextureOffset.X, bitmap.TextureOffset.Y, 0, bitmap.TextureSize.X, bitmap.TextureSize.Y, 1);
                    cmd.Upload(new FGTextureUploadDesc(dest, box, 0, 0), bitmap.Pixels);
                }
            }
        }

        private class PassData : IPassData
        {
            public UIFontManager? FontManager;

            public void Clear()
            {
                FontManager = null;
            }
        }
    }
}
