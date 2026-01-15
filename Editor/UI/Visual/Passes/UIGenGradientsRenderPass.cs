using CommunityToolkit.HighPerformance;
using Editor.UI.Datatypes;
using Primary.Assets;
using Primary.Common;
using Primary.Rendering;
using Primary.Rendering.Assets;
using Primary.Rendering.Recording;
using Primary.Rendering.Resources;
using Primary.Rendering.Structures;
using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.UI.Visual.Passes
{
    internal sealed class UIGenGradientsRenderPass : IRenderPass
    {
        private ComputeShaderAsset _shader;
        private PropertyBlock _dataBlock;

        public UIGenGradientsRenderPass()
        {
            _shader = AssetManager.LoadAsset<ComputeShaderAsset>("Editor/Shaders/EdGui/GenGradients.compute");
            _dataBlock = _shader.WaitIfNotLoaded().CreatePropertyBlock("CSGenLinearGradients")!;
        }

        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            UIGradientManager gradientManager = UIManager.Instance.Renderer.GradientManager;
            if (gradientManager.NeedsGradientsGenerated)
            {
                using (ComputePassDescription desc = renderPass.SetupComputePass<PassData>("UI-GenGradients", out PassData data))
                {
                    data.GradientManager = gradientManager;

                    data.Shader = _shader;
                    data.DataBlock = _dataBlock;

                    data.LinearGradients = desc.CreateBuffer(new FrameGraphBufferDesc
                    {
                        Width = (uint)(Unsafe.SizeOf<LinearGradientData>() * gradientManager.Gradients.Length),
                        Stride = Unsafe.SizeOf<LinearGradientData>(),
                        Usage = FGBufferUsage.GenericShader | FGBufferUsage.Structured
                    }, "UI-LiGrads");

                    int keyCount = 0;
                    foreach (ref readonly UIComputedGradient gradient in gradientManager.Gradients)
                        keyCount += gradient.Gradient.Keys.Length;

                    data.GradientKeys = desc.CreateBuffer(new FrameGraphBufferDesc
                    {
                        Width = (uint)(Unsafe.SizeOf<GradientKey>() * keyCount),
                        Usage = FGBufferUsage.GenericShader | FGBufferUsage.Raw
                    }, "UI-GradKeys");

                    data.Texture = desc.CreateTexture(new FrameGraphTextureDesc
                    {
                        Width = (int)gradientManager.GradientTextureSize.X,
                        Height = (int)gradientManager.GradientTextureSize.Y,
                        Format = RHIFormat.RGBA8_UNorm,
                        Usage = FGTextureUsage.PixelShader | FGTextureUsage.GenericShader | FGTextureUsage.ShaderResource | FGTextureUsage.UnorderedAccess
                    }, "UI-Gradients");

                    desc.UseResource(FGResourceUsage.ReadWrite, data.LinearGradients);
                    desc.UseResource(FGResourceUsage.ReadWrite, data.GradientKeys);
                    desc.UseResource(FGResourceUsage.Write, data.Texture);

                    desc.AllowPassCulling(false);
                    desc.SetRenderFunction<PassData>(PassFunction);
                }
            }
        }

        private static void PassFunction(ComputePassContext context, PassData data)
        {
            ComputeCommandBuffer cmd = context.CommandBuffer;

            ReadOnlySpan<UIComputedGradient> gradients = data.GradientManager!.Gradients;
            {
                int keyCount = 0;
                foreach (ref readonly UIComputedGradient gradient in gradients)
                    keyCount += gradient.Gradient.Keys.Length;

                using RentedArray<LinearGradientData> linearGradientDatas = RentedArray<LinearGradientData>.Rent(gradients.Length);
                using RentedArray<GradientKey> gradientKeys = RentedArray<GradientKey>.Rent(keyCount);

                for (int i = 0, j = 0; i < gradients.Length; ++i)
                {
                    ref readonly UIComputedGradient gradient = ref gradients.DangerousGetReferenceAt(i);

                    linearGradientDatas[i] = new LinearGradientData((uint)gradient.Region.Minimum.X, (uint)gradient.Region.Minimum.Y, (uint)j, (uint)gradient.Gradient.Keys.Length);
                    
                    for (int k = 0; k < gradient.Gradient.Keys.Length; ++k, ++j)
                    {
                        ref readonly UIGradientKey key = ref gradient.Gradient.Keys[k];
                        gradientKeys[j] = new GradientKey(key.Time, key.Color);
                    }
                }

                cmd.Upload(data.LinearGradients, linearGradientDatas.Span);
                cmd.Upload(data.GradientKeys, gradientKeys.Span);
            }

            if (data.Shader!.TryFindKernel("CSGenLinearGradients", out ComputeShaderKernel? kernel))
            {
                data.DataBlock!.SetResource(PropertyBlock.GetID("sbLinearGradients"), data.LinearGradients);
                data.DataBlock.SetResource(PropertyBlock.GetID("baGradientKeyBuffer"), data.GradientKeys);
                data.DataBlock.SetResource(PropertyBlock.GetID("txGradientOutput"), data.Texture);

                cmd.SetPipeline(kernel.Pipeline);
                cmd.SetProperties(data.DataBlock);

                cmd.Dispatch(
                    (uint)(UIGradientManager.LinearGradientWidth / kernel.ThreadSize.X),
                    (uint)(UIGradientManager.LinearGradientHeight / kernel.ThreadSize.Y),
                    (uint)gradients.Length);
            }
        }

        private class PassData : IPassData
        {
            public UIGradientManager? GradientManager;

            public ComputeShaderAsset? Shader;
            public PropertyBlock? DataBlock;

            public FrameGraphBuffer LinearGradients;
            public FrameGraphBuffer GradientKeys;

            public FrameGraphTexture Texture;

            public void Clear()
            {
                GradientManager = null;

                Shader = null;
                DataBlock = null;

                Texture = FrameGraphTexture.Invalid;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private readonly record struct LinearGradientData(uint OriginOffsetX, uint OriginOffsetY, uint KeyStartIndex, uint KeyCount);
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private readonly record struct GradientKey(float Time, Color Color);
    }
}
