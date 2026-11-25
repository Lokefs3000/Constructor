using System;
using System.Collections.Generic;
using System.Text;
using Primary.Rendering.Datatypes;

namespace Primary.Rendering.Playback
{
    internal interface IPlaybackReadback : IDisposable
    {
        public bool ReadFromResource(Span<byte> span);
    }
}
