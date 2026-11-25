using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtConsole.Communication
{
    internal static class CommLog
    {
        public static ILogger Net = Create("NET");
        public static ILogger Message = Create("MSG");

        private static ILogger Create(string prefix)
        {
            return new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: $"[{{Timestamp:HH:mm:ss}} {{Level:u3}}] [C{prefix}] {{Message:lj}}{{NewLine}}{{Exception}}")
#if DEBUG
                .MinimumLevel.Debug()
#endif
                .CreateLogger();
        }
    }
}
