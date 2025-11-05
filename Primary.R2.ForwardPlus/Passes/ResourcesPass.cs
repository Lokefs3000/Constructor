using Primary.Rendering2;
using Primary.Rendering2.Batching;
using Primary.Rendering2.Data;
using Primary.Rendering2.Recording;
using Primary.Rendering2.Resources;
using Primary.Rendering2.Structures;
using System.Runtime.CompilerServices;

namespace Primary.R2.ForwardPlus.Passes
{
    internal sealed class ResourcesPass : IRenderPass
    {
        public void SetupRenderPasses(RenderPass renderPass, RenderContextContainer context)
        {
            RenderStateData stateData = context.Get<RenderStateData>()!;
            ForwardPlusRenderPath renderPath = Unsafe.As<ForwardPlusRenderPath>(stateData.Path);

            RenderList list = renderPath.PrimaryRenderList!;

            GenericResources resources = renderPass.Blackboard.Add<GenericResources>();

            using (RasterPassDescription desc = renderPass.SetupRasterPass("Resources", out PassData passData))
            {
                resources.MatrixBuffer = desc.CreateBuffer(new FrameGraphBufferDesc
                {
                    Width = (uint)(list.TotalFlagCount * Unsafe.SizeOf<RenderFlag>()),
                    Stride = Unsafe.SizeOf<RenderFlag>(),
                    Usage = FGBufferUsage.GenericShader | FGBufferUsage.Structured
                });

                {
                    passData.MatrixBuffer = resources.MatrixBuffer;
                    passData.RenderList = list;
                }

                desc.UseResource(FGResourceUsage.Write, passData.MatrixBuffer);

                desc.SetRenderFunction<PassData>(ExecutePass);
            }
        }

        private static void ExecutePass(RasterPassContext context, PassData passData)
        {
            RasterCommandBuffer commandBuffer = context.CommandBuffer;

            {
                using FGMappedSubresource<RenderFlag> flags = commandBuffer.Map<RenderFlag>(passData.MatrixBuffer);

                Span<RenderFlag> tempFlags = flags.Span;
                foreach (ShaderRenderBatcher renderBatcher in passData.RenderList!.ShaderBatchers)
                {
                    renderBatcher.Flags.CopyTo(tempFlags);
                    tempFlags = tempFlags.Slice(renderBatcher.Flags.Length);
                }
            }
        }

        private class PassData : IPassData
        {
            public FrameGraphBuffer MatrixBuffer;
            public RenderList? RenderList;

            public void Clear()
            {
                MatrixBuffer = FrameGraphBuffer.Invalid;
                RenderList = null;
            }
        }
    }

    internal class GenericResources : IBlackboardData
    {
        public FrameGraphBuffer MatrixBuffer;

        public void Clear()
        {
            MatrixBuffer = FrameGraphBuffer.Invalid;
        }
    }
}
