using System;
using System.Collections.Generic;
using System.Text;
using Primary.Assets;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Data;

namespace VoxelizationDemo.Rendering.Compositing.Effects
{
    public sealed class TonemappingEffect : RenderingEffect
    {
        private readonly Data _data;
        private readonly Pass _pass;

        private float _tonemapperObjWeight;

        public TonemappingEffect()
        {
            _data = new Data();
            _pass = new Pass(this, _data);

            _tonemapperObjWeight = 0.0f;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _pass.Dispose();
                }

                _disposedValue = true;
            }
        }

        protected internal override void Clear()
        {
            _data.Tonemapper = Tonemapper.GT7;

            _tonemapperObjWeight = 0.0f;
        }

        protected internal override void Blend(RenderingEffectData data, float weight)
        {
            Data selfData = (Data)data;

            if (weight > _tonemapperObjWeight)
            {
                _data.Tonemapper = selfData.Tonemapper;
                _tonemapperObjWeight = weight;
            }
        }

        protected internal override void FlushPasses(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context)
        {
            manager.PushRenderPass(_pass);
        }

        private sealed class Pass : IRenderPass, IDisposable
        {
            private readonly TonemappingEffect _effect;
            private readonly Data _data;

            private readonly ShaderAsset _gt7Shader;
            private readonly PropertyBlock _gt7Properties;

            public Pass(TonemappingEffect effect, Data data)
            {
                _effect = effect;
                _data = data;

                _gt7Shader = AssetManager.LoadAsset<ShaderAsset>("Content/Shaders/Effects/GT7Tonemap.shader");
                _gt7Properties = _gt7Shader.CreatePropertyBlock();
            }

            public void Dispose()
            {
                _gt7Properties.Dispose();
            }

            public void SetupPasses(FrameGraphBuilder frameGraph)
            {
                (ShaderAsset? shader, PropertyBlock? properties) = _data.Tonemapper switch
                {
                    Tonemapper.GT7 => (_gt7Shader, _gt7Properties),
                    _ => (null, null)
                };

                if (shader == null || properties == null || !shader.IsReady)
                    return;

                properties.SetSingle("ppWeight", _effect._tonemapperObjWeight);

                RenderCameraData cameraData = frameGraph.GetContext<RenderCameraData>();
                RenderWindowData windowData = frameGraph.GetContext<RenderWindowData>();

                using (RasterPassBuilder builder = frameGraph.AddRasterPass("Tonemap", out PassData passData))
                {
                    passData.Shader = shader;
                    passData.Properties = properties;

                    passData.InColor = cameraData.ColorTexture;
                    passData.OutColor = builder.CreateTexture(new(frameGraph.GetDescription(cameraData.ColorTexture)) { Format = Primary.RHI.RHIFormat.RG11B10_Float });

                    builder.Read(passData.InColor);
                    builder.Write(passData.OutColor);
                    builder.SetFunction(PassFunction);

                    cameraData.ColorTexture = passData.OutColor;
                    windowData.ColorTexture = passData.OutColor;
                }
            }

            private static void PassFunction(RasterPassContext context)
            {
                RasterCommandBuffer cmd = context.CommandBuffer;
                PassData passData = context.GetPassData<PassData>()!;

                passData.Properties!.SetResource("txSourceImage", passData.InColor);

                cmd.SetRenderTarget(passData.OutColor);

                cmd.SetShader(passData.Shader!);
                cmd.SetProperties(passData.Properties!);

                cmd.DrawInstanced(3, 1, 0, 0);
            }

            private sealed class PassData : IPassData
            {
                public ShaderAsset? Shader;
                public PropertyBlock? Properties;

                public TextureHandle InColor;
                public TextureHandle OutColor;

                void IPassData.Clear()
                {
                    Shader = null;
                    Properties = null;

                    InColor = TextureHandle.Null;
                    OutColor = TextureHandle.Null;
                }
            }
        }

        public sealed class Data : RenderingEffectData
        {
            public Tonemapper Tonemapper = Tonemapper.GT7;
        }
    }

    public enum Tonemapper : byte
    {
        GT7 = 0
    }
}
