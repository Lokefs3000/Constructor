using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Interaction
{
    public interface ISelectionPolicy
    {
        public bool IsSelectedObjectValid(object selected);
    }
}
