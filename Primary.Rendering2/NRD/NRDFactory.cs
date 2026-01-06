using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering2.NRD
{
    internal static class NRDFactory
    {
        internal static INativeRenderDispatcher Create(RenderingManager manager, RHIDevice device)
        {
            return device.DeviceAPI switch
            {
                RHIDeviceAPI.Direct3D12 => new D3D12.NRDDevice(manager, device),
                _ => throw new NotSupportedException(device.DeviceAPI.ToString())
            };
        }
    }
}
