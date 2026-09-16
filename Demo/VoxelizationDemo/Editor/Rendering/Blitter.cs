using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Rendering.Assets;
using Primary.Rendering.Commands;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI;

namespace VoxelizationDemo.Editor.Rendering
{
    internal static class Blitter
    {
        private static readonly ShaderAsset _blitShader;
        private static readonly PropertyBlock _blitBlock;

        static Blitter()
        {
            _blitShader = AssetManager.LoadAsset<ShaderAsset>("Content/Shaders/Editor/Blit.shader");
            _blitBlock = _blitShader.CreatePropertyBlock();
        }

        public static void Blit(in RasterCommandBuffer cmd, RHITexture texture)
        {
            if (!_blitShader.IsLoaded)
                return;

            _blitBlock.SetResource("txTexture", texture);

            cmd.SetPipeline(_blitShader);
            cmd.SetProperties(_blitBlock);
            cmd.DrawInstanced(new FGDrawInstancedDesc(3));
        }

        public static void Blit(in RasterCommandBuffer cmd, FGTexture texture)
        {
            if (!_blitShader.IsLoaded)
                return;

            _blitBlock.SetResource("txTexture", texture);

            cmd.SetPipeline(_blitShader);
            cmd.SetProperties(_blitBlock);
            cmd.DrawInstanced(new FGDrawInstancedDesc(3));
        }

        public static void Blit(in RasterCommandBuffer cmd, TextureAsset texture)
        {
            if (!_blitShader.IsLoaded)
                return;

            _blitBlock.SetResource("txTexture", texture.Status switch
            {
                ResourceStatus.Pending => AssetManager.Static.DebugTexLoading,
                ResourceStatus.Running => AssetManager.Static.DebugTexLoading,
                ResourceStatus.Error => AssetManager.Static.DebugTexError,
                _ => texture
            });

            cmd.SetPipeline(_blitShader);
            cmd.SetProperties(_blitBlock);
            cmd.DrawInstanced(new FGDrawInstancedDesc(3));
        }
    }
}
