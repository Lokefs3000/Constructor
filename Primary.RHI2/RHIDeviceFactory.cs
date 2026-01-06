using Primary.RHI2.Direct3D12;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.RHI2
{
    public static class RHIDeviceFactory
    {
        public static RHIDevice CreateDefaultApi(RHIDeviceDescription description, ILogger? logger)
        {
            if (OperatingSystem.IsWindows())
                return new D3D12RHIDevice(description, logger);

            throw new PlatformNotSupportedException();
        }
    }
}
