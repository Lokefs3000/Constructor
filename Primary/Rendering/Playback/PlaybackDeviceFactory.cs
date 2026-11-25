using System;
using System.Collections.Generic;
using System.Text;
using Primary.RHI;

namespace Primary.Rendering.Playback
{
    internal static class PlaybackDeviceFactory
    {
        internal static IPlaybackDevice CreateDevice(RHIDevice device) => device.DeviceAPI switch
        {
            RHIDeviceAPI.Direct3D12 => new D3D12.PlaybackDevice(device),
            _ => throw new NotSupportedException(device.DeviceAPI.ToString())
        };
    }
}
