using Serilog;

namespace Editor
{
    internal static class EdLog
    {
        public static readonly ILogger Assets = Create("Assets");
        public static readonly ILogger Gui = Create("Gui");
        public static readonly ILogger Reflection = Create("Refl");
        public static readonly ILogger Serialization = Create("Serialize");

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
