using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Threading
{
    internal sealed class MainThreadSynchronizationContext : SynchronizationContext
    {
        public MainThreadSynchronizationContext()
        {
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            base.Post(d, state);
        }
    }
}
