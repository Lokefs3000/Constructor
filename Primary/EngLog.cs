using Serilog;

namespace Primary
{
    public static class EngLog
    {
        public static readonly ILogger Assets = Create("Assets");
        internal static readonly ILogger Systems = Create("Systems");
        internal static readonly ILogger Scene = Create("Scene");
        internal static readonly ILogger Console = Create("Console");
        public static readonly ILogger Render = Create("Render");
        public static readonly ILogger NRD = Create("NRD");
        public static readonly ILogger RHI = Create("RHI");
        internal static readonly ILogger Core = Create("Core");

        private static ILogger Create(string prefix)
        {
            return new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: $"[{{Timestamp:HH:mm:ss}} {{Level:u3}}] [P{prefix}] {{Message:lj}}{{NewLine}}{{Exception}}")
#if DEBUG
                .MinimumLevel.Debug()
#endif
                .CreateLogger();
        }
    }
}
