using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Rendering
{
    public readonly record struct ReadbackRequest : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
