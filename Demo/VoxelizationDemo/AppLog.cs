using System;
using System.Collections.Generic;
using System.Text;
using Primary.Logging;
using Serilog;

namespace VoxelizationDemo
{
    internal static class AppLog
    {
        internal static readonly ILogger Primary = CreateLogger();
        internal static readonly ILogger Editor = CreateLogger();

        private static ILogger CreateLogger() => new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.Logbook()
#if DEBUG
            .MinimumLevel.Debug()
#endif
            .CreateLogger();
    }
}
