using Editor.Geometry.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Geometry
{
    public sealed class GeoBrush
    {
        private GeoTransform _transform;
        private IGeoShape? _shape;

        public GeoBrush(IGeoShape? shape = null)
        {
            _transform = GeoTransform.Default;
            _shape = shape;
        }

        public GeoBrush(GeoTransform transform, IGeoShape? shape = null)
        {
            _transform = transform;
            _shape = shape;
        }

        public ref GeoTransform Transform => ref _transform;
        public IGeoShape? Shape { get => _shape; set => _shape = value; }
    }
}
