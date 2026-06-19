using System;
using System.Collections.Generic;
using System.Text;
using Primary.Logging;
using Serilog;

namespace PrimaryEditor
{
    internal static class EdLog
    {
        internal static readonly ILogger Assets = CreateLogger();

        private static ILogger CreateLogger() => new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.Logbook()
#if DEBUG
            .MinimumLevel.Debug()
#endif
            .CreateLogger();
    }
}
