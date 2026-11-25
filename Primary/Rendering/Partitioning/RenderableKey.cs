using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Primary.Rendering.Partitioning
{
    public readonly record struct RenderableKey(ulong Value)
    {
        public RenderableKey(int materialUId, int modelUId, int meshUId) : this(((ulong)materialUId << MaterialShift) | ((ulong)modelUId << ModelShift) | ((ulong)meshUId << MeshShift))
        {
        }

        public int MaterialUId => (int)((Value >> MaterialShift) & MaterialMask);
        public int ModelUId => (int)((Value >> ModelUId) & ModelMask);
        public int MeshUId => (int)((Value >> MeshUId) & MeshMask);

        public const int MaterialShift = 40;
        public const int MaterialMask = 0xffffff;

        public const int ModelShift = 16;
        public const int ModelMask = 0xffffff;

        public const int MeshShift = 0;
        public const int MeshMask = 0xffff;
    }
}
