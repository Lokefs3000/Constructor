using System;
using System.Collections.Generic;
using System.Text;
using Serilog;

namespace PrimaryEditor
{
    internal static class EdLog
    {
        internal static readonly ILogger Assets = CreateLogger();

        private static ILogger CreateLogger() => new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
    }
}
