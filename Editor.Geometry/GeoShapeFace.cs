using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Geometry
{
    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public struct GeoShapeFace
    {
        [FieldOffset(0)]
        public readonly GeoFaceShapeType Type;

        [FieldOffset(1)]
        public uint MaterialIndex;

        [FieldOffset(5)]
        public GeoShapeTriangle Triangle;

        [FieldOffset(5)]
        public GeoShapeQuad Quad;

        public GeoShapeFace(uint materialIndex, GeoShapeTriangle triangle)
        {
            Type = GeoFaceShapeType.Triangle;
            MaterialIndex = materialIndex;
            Triangle = triangle;
        }

        public GeoShapeFace(uint materialIndex, GeoShapeQuad quad)
        {
            Type = GeoFaceShapeType.Quad;
            MaterialIndex = materialIndex;
            Quad = quad;
        }
    }

    public enum GeoFaceShapeType : byte
    {
        Triangle = 0,
        Quad
    }

    /// <inheritdoc cref="GeoTriangle"/>
    public struct GeoShapeTriangle
    {
        public Vector2 UV0;
        public Vector2 UV1;
        public Vector2 UV2;

        public GeoShapeTriangle(Vector2 uv0, Vector2 uv1, Vector2 uv2)
        {
            UV0 = uv0;
            UV1 = uv1;
            UV2 = uv2;
        }

        public static readonly GeoShapeTriangle Default = new GeoShapeTriangle(Vector2.Zero, Vector2.UnitY, Vector2.One);
    }

    /// <inheritdoc cref="GeoQuad"/>
    public struct GeoShapeQuad
    {
        public Vector2 UV0;
        public Vector2 UV1;
        public Vector2 UV2;
        public Vector2 UV3;

        public GeoShapeQuad(Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            UV0 = uv0;
            UV1 = uv1;
            UV2 = uv2;
            UV3 = uv3;
        }

        public static readonly GeoShapeQuad Default = new GeoShapeQuad(Vector2.Zero, Vector2.UnitX, Vector2.UnitY, Vector2.One);
    }
}
