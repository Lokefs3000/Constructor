using Primary.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Inspector.Editors
{
    public abstract class ObjectEditor
    {
        public abstract void SetupInspectorFields(object obj);
        public abstract void DrawInspector();
    }
}
