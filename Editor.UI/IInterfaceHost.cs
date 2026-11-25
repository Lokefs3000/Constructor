using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI
{
    public interface IInterfaceHost
    {
        public UIStateFlags InvalidationFlags { get; }

        public void Update();

        public void AddStateFlags(UIStateFlags flags);
        public void RemoveStateFlags(UIStateFlags flags);
    }
}
