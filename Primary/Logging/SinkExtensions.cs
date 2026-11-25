using System;
using System.Collections.Generic;
using System.Text;
using Serilog;
using Serilog.Configuration;

namespace Primary.Logging
{
    public static class SinkExtensions
    {
        extension(LoggerSinkConfiguration configuration)
        {
            public LoggerConfiguration Logbook()
            {
                return configuration.Sink(new LogbookSink());
            }
        }
    }
}
