using Editor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.GeoEdit
{
    internal interface IGeoTool
    {
        public void ConnectEvents();
        public void DisconnectEvents();

        public void Update();
        public void Render(ref readonly GeoToolRenderInterface @interface);
    }
}
