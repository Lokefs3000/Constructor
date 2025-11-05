using Primary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Debuggable
{
    public interface IDebugRenderer
    {
        public void DrawLine(Vector3 from, Vector3 to, Vector4 color);
        public void DrawVector(Vector3 position);
        public void DrawWireSphere(Vector3 center, float radius, Vector4 color);
        public void DrawWireBox(Vector3 center, Vector3 extents, Vector4 color);
        public void DrawWireAABB(AABB aabb, Vector4 color);
    }
}
