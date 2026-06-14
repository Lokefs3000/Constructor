using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Interaction.Draw
{
    public interface ISelectionDrawer
    {
        public void DrawObject(object obj, SelectionDrawList drawList);
    }
}
