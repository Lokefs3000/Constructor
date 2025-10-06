using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Geometry
{
    public sealed class GeoBrushScene
    {
        private List<GeoBrush> _brushes;

        internal GeoBrushScene()
        {
            _brushes = new List<GeoBrush>();
        }

        /// <summary>Not thread-safe</summary>
        public void AddBrush(GeoBrush brush) => _brushes.Add(brush);

        /// <summary>Not thread-safe</summary>
        public void RemoveBrush(GeoBrush brush) => _brushes.Remove(brush);

        public IReadOnlyList<GeoBrush> Brushes => _brushes;
    }
}
