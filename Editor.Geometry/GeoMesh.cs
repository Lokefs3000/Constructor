using Primary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Geometry
{
    public readonly struct GeoMesh
    {
        public readonly Vector3[] Vertices;
        public readonly GeoFace[] Faces;

        public readonly AABB Boundaries;

        public GeoMesh(Vector3[] vertices, GeoFace[] faces, AABB boundaries)
        {
            Vertices = vertices;
            Faces = faces;

            Boundaries = boundaries;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct GeoFace
    {
        [FieldOffset(0)]
        public readonly GeoFaceType Type;

        [FieldOffset(1)]
        public ushort ShapeFaceIndex;

        [FieldOffset(3)]
        public uint MaterialIndex;

        [FieldOffset(7)]
        public GeoTriangle Triangle;

        [FieldOffset(7)]
        public GeoQuad Quad;

        public GeoFace(ushort faceIndex, uint materialIndex, GeoTriangle tri)
        {
            Type = GeoFaceType.Triangle;
            ShapeFaceIndex = faceIndex;
            MaterialIndex = materialIndex;
            Triangle = tri;
        }

        public GeoFace(ushort faceIndex, uint materialIndex, GeoQuad quad)
        {
            Type = GeoFaceType.Quad;
            ShapeFaceIndex = faceIndex;
            MaterialIndex = materialIndex;
            Quad = quad;
        }

        public static implicit operator GeoFace(GeoTriangle tri) => new GeoFace(ushort.MaxValue, uint.MaxValue, tri);
        public static implicit operator GeoFace(GeoQuad quad) => new GeoFace(ushort.MaxValue, uint.MaxValue, quad);
    }

    public enum GeoFaceType : byte
    {
        Triangle = 0,
        Quad
    }

    /// <summary>
    /// <code>
    /// 0\
    /// |  \
    /// |    \
    /// 1 --- 2
    /// </code>
    /// </summary>
    public struct GeoTriangle
    {
        public GeoPoint Point0;
        public GeoPoint Point1;
        public GeoPoint Point2;

        public GeoTriangle(GeoPoint point0, GeoPoint point1, GeoPoint point2)
        {
            Point0 = point0;
            Point1 = point1;
            Point2 = point2;
        }

        public GeoTriangle(int point0, int point1, int point2, GeoShapeTriangle triangle = default)
        {
            Point0 = new GeoPoint(point0, triangle.UV0);
            Point1 = new GeoPoint(point1, triangle.UV1);
            Point2 = new GeoPoint(point2, triangle.UV2);
        }
    }

    /// <summary>
    /// <code>
    /// 0 --- 1
    /// | \   |
    /// |   \ |
    /// 2 --- 3
    /// </code>
    /// </summary>
    public struct GeoQuad
    {
        public GeoPoint Point0;
        public GeoPoint Point1;
        public GeoPoint Point2;
        public GeoPoint Point3;

        public GeoQuad(GeoPoint point0, GeoPoint point1, GeoPoint point2, GeoPoint point3)
        {
            Point0 = point0;
            Point1 = point1;
            Point2 = point2;
            Point3 = point3;
        }

        public GeoQuad(int point0, int point1, int point2, int point3, GeoShapeQuad quad = default)
        {
            Point0 = new GeoPoint(point0, quad.UV0);
            Point1 = new GeoPoint(point1, quad.UV1);
            Point2 = new GeoPoint(point2, quad.UV2);
            Point3 = new GeoPoint(point3, quad.UV3);
        }
    }

    public readonly record struct GeoPoint(int Index, Vector2 UV);
}
