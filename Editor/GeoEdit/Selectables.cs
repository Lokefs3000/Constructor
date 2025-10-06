using Editor.Geometry;
using Editor.Geometry.Shapes;
using Editor.Interaction;
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

    internal sealed class SelectedGeoBrush : SelectedGeoObjectBase
    {
        public readonly GeoBrush Brush;

        public SelectedGeoBrush(GeoBrush brush)
        {
            Brush = brush;
        }
    }

    internal sealed class SelectedGeoBoxShape : SelectedGeoObjectBase
    {
        public readonly GeoBrush Brush;
        public readonly GeoBoxShape Shape;
        public readonly int FaceIndex;

        public SelectedGeoBoxShape(GeoBrush brush, int faceIndex)
        {
            Debug.Assert(brush.Shape is GeoBoxShape);

            Brush = brush;
            Shape = (GeoBoxShape)brush.Shape!;
            FaceIndex = faceIndex;
        }
    }
}
