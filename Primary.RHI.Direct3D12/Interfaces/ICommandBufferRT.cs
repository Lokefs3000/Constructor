using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

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

        public bool HasInitialClearCompleted { get; set; }
    }
}
