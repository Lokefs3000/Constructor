using System;
using System.Collections.Generic;
using System.Text;
using Primary.Rendering.Recording;

namespace Primary.Rendering.Playback
{
    internal interface IPlaybackRecorder
    {
        public uint GetDescriptorIndex(DescriptorHeapType heapType);
    }
}
