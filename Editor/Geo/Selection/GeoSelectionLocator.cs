using Editor.Geometry;
using Editor.Interaction;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geo.Selection
{
    //[SelectionLocatorTypes(typeof(Brush), typeof(SelectedFace))]
    internal sealed class GeoSelectionLocator : ISelectionLocator
    {
        public SelectionGroup? Locate(SelectionManager selection, object obj)
        {
            return null;
        }
    }
}
