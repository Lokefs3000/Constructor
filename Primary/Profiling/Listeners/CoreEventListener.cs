using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Text;

namespace Primary.Profiling.Listeners
{
    internal sealed class CoreEventListener : EventListener
    {
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime"))
            {
                
                // and collect information pertaining to garbage collection.
                EnableEvents(eventSource, EventLevel.Informational, 0);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            base.OnEventWritten(eventData);
        }
    }
}
