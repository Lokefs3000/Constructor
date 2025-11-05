using Serilog;

namespace Primary
{
    internal static class EngLog
    {
        internal static readonly ILogger Assets = Create("Assets");
        internal static readonly ILogger Systems = Create("Systems");
        internal static readonly ILogger Scene = Create("Scene");
        internal static readonly ILogger Console = Create("Console");
        internal static readonly ILogger Render = Create("Render");

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
