using CommunityToolkit.Diagnostics;
using Primary.Collections.ReadOnly;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Geometry
{
    public sealed class BrushGroup
    {
        private readonly GeoScene _scene;
        private string _name;

        private List<Brush> _brushes;
        private Dictionary<int, Brush> _brushLookup;

        internal BrushGroup(GeoScene scene, string name)
        {
            _scene = scene;
            _name = name;

            _brushes = new List<Brush>();
            _brushLookup = new Dictionary<int, Brush>();
        }

        /// <summary>Not thread-safe</summary>
        public Brush CreateBrush()
        {
            Brush brush = new Brush(this, _scene.GetNewBrushId(), new BrushTransform());

            _brushes.Add(brush);
            _brushLookup.Add(brush.Id.LocalId, brush);

            _scene.RegisterNewBrush(brush);
            return brush;
        }

        /// <summary>Not thread-safe</summary>
        public Brush CreateBrushWithId(int id)
        {
            _scene.AddBrushId(id);

            Brush brush = new Brush(this, new BrushId(_scene.Id, id), new BrushTransform());

            _brushes.Add(brush);
            _brushLookup.Add(brush.Id.LocalId, brush);

            _scene.RegisterNewBrush(brush);
            return brush;
        }

        /// <summary>DANGEROUS: Not thread-safe</summary>
        public Brush AddBrush(Brush brush)
        {
            Guard.Equals(brush.Group, this);

            _scene.AddBrushId(brush.Id.LocalId);

            _brushes.Add(brush);
            _brushLookup.Add(brush.Id.LocalId, brush);

            _scene.RegisterNewBrush(brush);
            return brush;
        }

        /// <summary>Not thread-safe</summary>
        public void DestroyBrush(Brush brush)
        {
            if (_brushLookup.TryGetValue(brush.Id.LocalId, out Brush? brushFromId) && brushFromId == brush)
            {
                _brushes.Remove(brush);
                _brushLookup.Remove(brush.Id.LocalId);

                _scene.ReturnOldBrushId(brush.Id);
                _scene.RegisterBrushDeletion(brush);
            }
        }

        /// <summary>Not thread-safe</summary>
        public bool TryGetBrush(int localId, out Brush? brush) => _brushLookup.TryGetValue(localId, out brush);
        /// <summary>Not thread-safe</summary>
        public bool TryGetBrush(BrushId id, out Brush? brush) => _brushLookup.TryGetValue(id.LocalId, out brush);

        public GeoScene Scene => _scene;
        public string Name => _name;

        public ROList<Brush> Brushes => _brushes;
    }
}
