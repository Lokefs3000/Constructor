using Primary.Assets;
using Primary.Rendering.Pass;
using Primary.Rendering.Raw;
using Primary.Serialization.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Primary.Rendering.PostProcessing
{
    internal sealed class EnviormentEffect : PostProcessingEffect<EnviormentEffectData>
    {
        private ShaderAsset _skyboxShader;
        private ShaderBindGroup _bindGroup;

        internal EnviormentEffect()
        {
            _skyboxShader = AssetManager.LoadAsset<ShaderAsset>("Engine/Shaders/DefaultSkybox.hlsl");
            _bindGroup = _skyboxShader.CreateDefaultBindGroup();
        }

        public override void Dispose() { }

        public override void SetupPass(RenderPass renderPass, EnviormentEffectData data)
        {
            if (data.Skybox == null)
                return;

            using (RasterPassDescription rasterPass = renderPass.CreateRasterPass())
            {
                rasterPass.SetThreadingPolicy(RenderPassThreadingPolicy.None);
                rasterPass.SetUserArgument(data);
                rasterPass.SetFunction(PassFunction);
            }
        }

        private void PassFunction(RasterCommandBuffer commandBuffer, RenderPassData passData, object? userArg)
        {
            Debug.Assert(userArg != null);

            EnviormentEffectData effectData = Unsafe.As<EnviormentEffectData>(userArg!);
            RenderPassViewportData viewportData = passData.Get<RenderPassViewportData>()!;

            _bindGroup.SetResource("txSkybox", effectData.Skybox!);

            commandBuffer.SetRenderTarget(viewportData.CameraRenderTarget, true);
            commandBuffer.SetScissorRect(new RHI.ScissorRect(0, 0, 10000, 10000));

            commandBuffer.SetShader(_skyboxShader);
            commandBuffer.SetConstants(Matrix4x4.CreateTranslation(viewportData.ViewPosition) * viewportData.VP);
            commandBuffer.SetBindGroups(_bindGroup);
            commandBuffer.CommitShaderResources();

            commandBuffer.DrawInstanced(new RHI.DrawInstancedArgs(36));
        }
    }

    public sealed class EnviormentEffectData : IPostProcessingData
    {
        public TextureAsset? Skybox;

        [JsonIgnore]
        public string Name => "Enviorment";
    }
}
