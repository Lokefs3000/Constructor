using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering2.NRD
{
    internal static class NRDFactory
    {
        internal static INativeRenderDispatcher Create(RenderingManager manager, RHI.GraphicsDevice device)
        {
            return device.API switch
            {
                RHI.GraphicsAPI.Direct3D12 => new D3D12.NRDDevice(manager, device),
                _ => throw new NotSupportedException(device.API.ToString())
            };
        }
    }
}
