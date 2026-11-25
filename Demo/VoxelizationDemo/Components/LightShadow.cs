using System;
using System.Collections.Generic;
using System.Text;
using Primary.Components;

namespace VoxelizationDemo.Components
{
    [Component]
    public struct LightShadow : IComponent
    {
        private int _resolution;

        public LightShadow()
        {
            _resolution = 256;
        }

        public int Resolution { get => _resolution; set => _resolution = value; }
    }
}
