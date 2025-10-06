using Vortice.Direct3D12;

namespace Primary.RHI.Direct3D12.Interfaces
{
    internal interface ICommandBufferRT
    {
        public ICommandBufferRTView? ColorTexture { get; }
        public ICommandBufferRTView? DepthTexture { get; }

        public RenderTargetFormat RTFormat { get; }
        public DepthStencilFormat DSTFormat { get; }

        public Vortice.Mathematics.Viewport Viewport { get; }
    }

    internal interface ICommandBufferRTView : ICommandBufferResource
    {
        public CpuDescriptorHandle ViewCpuDescriptor { get; }
        public ID3D12Resource Resource { get; }

        public bool HasInitialClearCompleted { get; set; }

        public void CopyTexture(ID3D12GraphicsCommandList7 commandList, ICommandBufferRTView view);
    }
}
