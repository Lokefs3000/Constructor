using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Geometry
{
    public struct GeoTransform
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public Vector3 Origin;

        public GeoTransform(Vector3 position, Quaternion rotation, Vector3 origin)
        {
            Position = position;
            Rotation = rotation;

            Origin = origin;
        }

        public static readonly GeoTransform Default = new GeoTransform(Vector3.Zero, Quaternion.Identity, Vector3.Zero);
    }
}
