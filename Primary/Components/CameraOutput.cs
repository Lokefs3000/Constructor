using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Primary.Components
{
    [Component]
    [ComponentRequirements(typeof(Camera))]
    public record struct CameraOutput : IComponent
    {
        private uint _windowId;

        public CameraOutput()
        {
            _windowId = uint.MaxValue;
        }

        public uint WindowId { get => _windowId; set => _windowId = value; }
    }
}
