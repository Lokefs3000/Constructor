using System;
using System.Collections.Generic;
using System.Text;
using Primary.Logging;
using Serilog;

namespace PrimaryEditor
{
    internal static class EdLog
    {
        internal static readonly ILogger Testing = CreateLogger();
        internal static readonly ILogger Assets = CreateLogger();
        internal static readonly ILogger Reflection = CreateLogger();
        internal static readonly ILogger Inspector = CreateLogger();

        private static ILogger CreateLogger() => new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.Logbook()
#if DEBUG
            .MinimumLevel.Debug()
#endif
            .CreateLogger();
    }
}
