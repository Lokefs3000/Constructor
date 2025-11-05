using Primary.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Geometry
{
    public struct GeoShapeFace
    {
        public readonly GeoFaceShapeType Type;
        public MaterialAsset? MaterialIndex;

        public GeoShapeTriangle Triangle { get => _union.Triangle; set => _union.Triangle = value; }
        public GeoShapeQuad Quad { get => _union.Quad; set => _union.Quad = value; }

        private __Union _union;

        public GeoShapeFace(MaterialAsset? materialIndex, GeoShapeTriangle triangle)
        {
            Type = GeoFaceShapeType.Triangle;
            MaterialIndex = materialIndex;
            Triangle = triangle;
        }

        public GeoShapeFace(MaterialAsset? materialIndex, GeoShapeQuad quad)
        {
            Type = GeoFaceShapeType.Quad;
            MaterialIndex = materialIndex;
            Quad = quad;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct __Union
        {
            [FieldOffset(0)]
            public GeoShapeTriangle Triangle;
            [FieldOffset(0)]
            public GeoShapeQuad Quad;
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
