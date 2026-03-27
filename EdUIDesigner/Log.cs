using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace EdUIDesigner
{
    internal static class Log
    {
        internal static ILogger EdUI = Create("EdUI");

        private static ILogger Create(string prefix)
        {
            return new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: $"[{{Timestamp:HH:mm:ss}} {{Level:u3}}] [E{prefix}] {{Message:lj}}{{NewLine}}{{Exception}}")
#if DEBUG
                .MinimumLevel.Debug()
#endif
                .CreateLogger();
        }
    }
}
