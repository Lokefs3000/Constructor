using System;
using System.Collections.Generic;
using System.Text;
using Serilog.Core;
using Serilog.Events;

namespace Primary.Logging
{
    public sealed class LogbookSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            string str = logEvent.RenderMessage();
            Logbook.LogNewMessage(str, logEvent.Exception);
        }
    }
}
