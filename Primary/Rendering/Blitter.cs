using Primary.Assets;
using Primary.Common;
using Primary.Rendering.Raw;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering
{
    public sealed class Blitter
    {
        private static Blitter? s_instance = null;
        private static Blitter Instance => NullableUtility.ThrowIfNull(s_instance);

        private ShaderAsset _blitShader;
        private ShaderBindGroup _bindGroup;

        internal Blitter()
        {
            s_instance = this;

            _blitShader = NullableUtility.ThrowIfNull(AssetManager.LoadAsset<ShaderAsset>("Hidden/Blit", true));
            _bindGroup = _blitShader.CreateDefaultBindGroup();
        }

        private void BlitInternal(RasterCommandBuffer commandBuffer, ShaderAsset shader, RHI.Resource resource, Vector2 offset, Vector2 scale)
        {
            bool r = _bindGroup.SetResource("txTexture", resource);
            Debug.Assert(r);

            commandBuffer.SetShader(_blitShader);
            commandBuffer.SetBindGroups(_bindGroup);
            commandBuffer.SetConstants(new ConstantsData { Offset = offset, Scale = scale });
            commandBuffer.CommitShaderResources();
            commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs(3));
        }

        public static void Blit(RasterCommandBuffer commandBuffer, RHI.Texture texture) => Instance.BlitInternal(commandBuffer, Instance._blitShader, texture, Vector2.Zero, Vector2.One);
        public static void Blit(RasterCommandBuffer commandBuffer, RHI.RenderTextureView textureView) => Instance.BlitInternal(commandBuffer, Instance._blitShader, textureView, Vector2.Zero, Vector2.One);

        public static void Blit(RasterCommandBuffer commandBuffer, RHI.Texture texture, Vector2 offset, Vector2 scale) => Instance.BlitInternal(commandBuffer, Instance._blitShader, texture, offset, scale);
        public static void Blit(RasterCommandBuffer commandBuffer, RHI.RenderTextureView textureView, Vector2 offset, Vector2 scale) => Instance.BlitInternal(commandBuffer, Instance._blitShader, textureView, offset, scale);

        public static void Blit(RasterCommandBuffer commandBuffer, ShaderAsset shader, RHI.Texture texture) => Instance.BlitInternal(commandBuffer, shader, texture, Vector2.Zero, Vector2.One);
        public static void Blit(RasterCommandBuffer commandBuffer, ShaderAsset shader, RHI.RenderTextureView textureView) => Instance.BlitInternal(commandBuffer, shader, textureView, Vector2.Zero, Vector2.One);

        public static void Blit(RasterCommandBuffer commandBuffer, ShaderAsset shader, RHI.Texture texture, Vector2 offset, Vector2 scale) => Instance.BlitInternal(commandBuffer, shader, texture, offset, scale);
        public static void Blit(RasterCommandBuffer commandBuffer, ShaderAsset shader, RHI.RenderTextureView textureView, Vector2 offset, Vector2 scale) => Instance.BlitInternal(commandBuffer, shader, textureView, offset, scale);

        private struct ConstantsData
        {
            public Vector2 Offset;
            public Vector2 Scale;
        }
    }
}
