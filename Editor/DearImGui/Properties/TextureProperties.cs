using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui.Properties
{
    internal sealed class TextureProperties : IObjectPropertiesViewer
    {
        internal TextureProperties()
        {

        }

        public void Render(object target)
        {
            TargetData td = (TargetData)target;
        }

        internal record class TargetData(string FullPath);
    }
}
