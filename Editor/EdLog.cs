using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor
{
    internal static class EdLog
    {
        public static readonly ILogger Assets = Create("Assets");
        public static readonly ILogger Gui = Create("Gui");

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
