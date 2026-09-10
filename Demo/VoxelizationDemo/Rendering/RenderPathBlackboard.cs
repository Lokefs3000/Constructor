using System;
using System.Collections.Generic;
using System.Text;
using Primary.Rendering;
using Primary.Rendering.Batching;

namespace VoxelizationDemo.Rendering
{
    public sealed class RenderPathBlackboard : IBlackboardData
    {
        public RenderList? RenderList;
        public LightCollector? LightCollector;

        public void Clear()
        {
            RenderList = null;
            LightCollector = null;
        }
    }
}
