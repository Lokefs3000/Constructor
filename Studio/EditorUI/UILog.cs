using System;
using System.Collections.Generic;
using System.Text;
using Serilog;

namespace EditorUI
{
    public static class UILog
    {
        internal static ILogger? Logger { get; private set; }

        internal static void CreateDefault() => Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
    }
}
