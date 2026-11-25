using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Inspector.V2
{
    public abstract class InspectorContext
    {
        public InspectorContext()
        {
            
        }

        public abstract void UpdateValues();
    }
}
