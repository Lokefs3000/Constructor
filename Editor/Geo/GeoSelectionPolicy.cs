using Editor.Geometry;
using Editor.Interaction;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geo
{
    internal sealed class GeoSelectionPolicy : ISelectionPolicy
    {
        public bool IsSelectedObjectValid(object obj)
        {
            return obj is Brush or SelectedFace;
        }
    }
}
