using Primary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Geometry.Shapes
{
    public sealed class GeoBoxShape : IGeoShape
    {
        private GeoShapeFace[] _shapeFaces;

        private Vector3[]? _vertices;
        private GeoFace[]? _faces;

        private bool _isModified;

        private Vector3 _extents;

        public GeoBoxShape()
        {
            _shapeFaces = new GeoShapeFace[6];

            Array.Fill(_shapeFaces, new GeoShapeFace(uint.MaxValue, GeoShapeQuad.Default));
        }

        public GeoMesh GenerateMesh()
        {
            if (_vertices == null || _faces == null)
            {
                _vertices = new Vector3[8];
                _faces = new GeoFace[6];

                _isModified = true;
            }

            if (_isModified)
            {
                //vertices
                {
                    /*
                      4 ----- 5
                     /|      /|
                    / |     / |
                    0 ---- 1  |
                    | 6 ---|- 7
                    |/     | /
                    2 ---- 3
                    
                      <+X->

                    */

                    //Front
                    _vertices[0] = new Vector3(0.0f, _extents.Y, 0.0f);
                    _vertices[1] = new Vector3(_extents.X, _extents.Y, 0.0f);
                    _vertices[2] = new Vector3(0.0f, 0.0f, 0.0f);
                    _vertices[3] = new Vector3(_extents.X, 0.0f, 0.0f);

                    //Back
                    _vertices[4] = new Vector3(0.0f, _extents.Y, _extents.Z);
                    _vertices[5] = new Vector3(_extents.X, _extents.Y, _extents.Z);
                    _vertices[6] = new Vector3(0.0f, 0.0f, _extents.Z);
                    _vertices[7] = new Vector3(_extents.X, 0.0f, _extents.Z);
                }

                //faces
                {
                    _faces[0] = new GeoFace(0, _shapeFaces[0].MaterialIndex, new GeoQuad(4, 0, 6, 2, _shapeFaces[0].Quad)); //Left
                    _faces[1] = new GeoFace(1, _shapeFaces[1].MaterialIndex, new GeoQuad(1, 5, 3, 7, _shapeFaces[1].Quad)); //Right
                    _faces[2] = new GeoFace(2, _shapeFaces[2].MaterialIndex, new GeoQuad(5, 4, 7, 6, _shapeFaces[2].Quad)); //Front
                    _faces[3] = new GeoFace(3, _shapeFaces[3].MaterialIndex, new GeoQuad(0, 1, 2, 3, _shapeFaces[3].Quad)); //Back
                    _faces[4] = new GeoFace(4, _shapeFaces[4].MaterialIndex, new GeoQuad(4, 5, 0, 1, _shapeFaces[4].Quad)); //Top
                    _faces[5] = new GeoFace(5, _shapeFaces[5].MaterialIndex, new GeoQuad(7, 6, 3, 2, _shapeFaces[5].Quad)); //Bottom
                }

                _isModified = false;
            }

            return new GeoMesh(_vertices, _faces, new AABB(Vector3.Zero, _extents));
        }

        public void ForceDirty()
        {
            _isModified = true;
        }

        public Span<GeoShapeFace> Faces => _shapeFaces.AsSpan();

        public bool IsDirty => _isModified;

        public Vector3 Extents
        {
            get => _extents;
            set
            {
                if (_extents != value)
                    _isModified = true;
                _extents = value;
            }
        }
    }

    public enum GeoBoxShapeFace : byte
    {
        Left = 0,
        Right,
        Front,
        Back,
        Top,
        Bottom,
    }
}
