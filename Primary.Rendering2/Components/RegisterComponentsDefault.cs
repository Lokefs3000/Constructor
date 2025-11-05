using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering2.Components
{
    public static class RegisterComponentsDefault
    {
        public static void RegisterDefault()
        {
            SceneEntityManager.Register<RenderOctantInfo>();
        }
    }
}
