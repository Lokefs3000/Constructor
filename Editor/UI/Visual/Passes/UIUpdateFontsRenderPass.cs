using Primary.Rendering;
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
                using (RasterPassDescription desc = renderPass.SetupRasterPass<PassData>("UI-UpdFonts", out PassData data))
                {
                    data.FontManager = fontManager;

                    //foreach (UIFontUpdate update in data.FontManager.PendingFontUpdates)
                    //{
                    //    desc.UseResource(FGResourceUsage.Write, update.Style.AtlasTexture!);
                    //    if (update.OldAtlas != null)
                    //        desc.UseResource(FGResourceUsage.Read, update.OldAtlas);
                    //}

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
                if (update.OldAtlas != null)
                {
                    cmd.Copy(new FGTextureCopyDesc(
                        new FGTextureCopySource(update.OldAtlas, 0), null,
                        new FGTextureCopySource(dest, 0), 0, 0, 0));
                }

                foreach (UIGlyphLine line in update.NewLines)
                {
                    FGBox box = new FGBox((int)line.TextureOrigin.X, (int)line.TextureOrigin.Y, 0, (int)line.BitmapSize.X, (int)line.BitmapSize.Y, 1);
                    cmd.Upload(new FGTextureUploadDesc(dest, box, 0, 0), line.BitmapData);
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
