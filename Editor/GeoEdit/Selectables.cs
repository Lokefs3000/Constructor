using Editor.Geometry;
using Editor.Geometry.Shapes;
using Editor.Interaction;
using Editor.Interaction.Logic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.GeoEdit
{
    internal abstract class SelectedGeoObjectBase : SelectedBase
    {

    }

    internal abstract class SelectedGeoShapeBase : SelectedGeoObjectBase
    {
        public readonly GeoBrush Brush;

        internal SelectedGeoShapeBase(GeoBrush brush)
        {
            Brush = brush;
        }
    }

    [SelectionLogic(typeof(GeoSelectionLogic))]
    internal sealed class SelectedGeoBrush : SelectedGeoObjectBase
    {
        public readonly GeoBrush Brush;

        public SelectedGeoBrush(GeoBrush brush)
        {
            Brush = brush;
        }
    }

    [SelectionLogic(typeof(GeoSelectionLogic))]
    internal sealed class SelectedGeoBoxShape : SelectedGeoShapeBase
    {
        public readonly GeoBoxShape Shape;
        public readonly int FaceIndex;

        public SelectedGeoBoxShape(GeoBrush brush, int faceIndex) : base(brush)
        {
            Debug.Assert(brush.Shape is GeoBoxShape);

            Shape = (GeoBoxShape)brush.Shape!;
            FaceIndex = faceIndex;
        }
    }
}
