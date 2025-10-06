using Editor.Assets.Types;
using Editor.Geometry;
using Editor.Geometry.Shapes;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.GeoEdit
{
    internal sealed class GeoPicker
    {
        public static bool Pick(Ray ray, GeoSceneAsset geoScene, bool includeGrid, out GeoPickResult result)
        {
            result = default;

            if (includeGrid)
            {
                InfinitePlane plane = new InfinitePlane(Vector3.Zero, Vector3.UnitY);
                float t = InfinitePlane.Intersect(plane, ray);
                if (t >= 0.0f)
                {
                    result = new GeoPickResult(ray.AtDistance(t), Vector3.UnitY, null, -1);
                    return true;
                }
            }

            return false;
        }

        public static bool RaycastFaceWithinShape(Ray ray, IGeoShape shape, GeoTransform transform, out GeoShapePickResult result)
        {
            GeoMesh mesh = shape.GenerateMesh();

            bool hasRotation = !transform.Rotation.IsIdentity;
            Vector3 pivotOffset = transform.Position + Vector3.Lerp(mesh.Boundaries.Minimum, mesh.Boundaries.Maximum, transform.Origin);

            for (int i = 0; i < mesh.Faces.Length; i++)
            {
                ref GeoFace face = ref mesh.Faces[i];

                Vector3? hit = null;
                Vector3 normal = Vector3.Zero;

                if (face.Type == GeoFaceType.Triangle)
                {
                    ref GeoTriangle tri = ref face.Triangle;

                    Vector3 v0 = mesh.Vertices[tri.Point0.Index] + pivotOffset;
                    Vector3 v1 = mesh.Vertices[tri.Point1.Index] + pivotOffset;
                    Vector3 v2 = mesh.Vertices[tri.Point2.Index] + pivotOffset;

                    if (hasRotation)
                    {
                        v0 = Vector3.Transform(v0, transform.Rotation);
                        v1 = Vector3.Transform(v1, transform.Rotation);
                        v2 = Vector3.Transform(v2, transform.Rotation);
                    }

                    Triangle tempTri = new Triangle(v0, v1, v2);
                    hit = GeoMathUtility.IntersectTriangle(in ray, in tempTri);

                    if (hit.HasValue)
                    {
                        Vector3 edge1 = v1 - v0;
                        Vector3 edge2 = v2 - v0;

                        normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
                    }
                }
                else
                {
                    ref GeoQuad quad = ref face.Quad;

                    Vector3 v0 = mesh.Vertices[quad.Point0.Index] + pivotOffset;
                    Vector3 v1 = mesh.Vertices[quad.Point1.Index] + pivotOffset;
                    Vector3 v2 = mesh.Vertices[quad.Point2.Index] + pivotOffset;
                    Vector3 v3 = mesh.Vertices[quad.Point3.Index] + pivotOffset;

                    if (hasRotation)
                    {
                        v0 = Vector3.Transform(v0, transform.Rotation);
                        v1 = Vector3.Transform(v1, transform.Rotation);
                        v2 = Vector3.Transform(v2, transform.Rotation);
                        v3 = Vector3.Transform(v3, transform.Rotation);
                    }

                    Triangle tempTri1 = new Triangle(v3, v2, v0);
                    Triangle tempTri2 = new Triangle(v1, v3, v0);

                    hit = GeoMathUtility.IntersectTriangle(in ray, in tempTri1);
                    if (hit.HasValue)
                    {
                        Vector3 edge1 = v2 - v3;
                        Vector3 edge2 = v0 - v3;

                        normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
                    }
                    else
                    {
                        hit = GeoMathUtility.IntersectTriangle(in ray, in tempTri2);
                        if (hit.HasValue)
                        {
                            Vector3 edge1 = v3 - v1;
                            Vector3 edge2 = v0 - v1;

                            normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
                        }
                    }
                }

                if (hit.HasValue)
                {
                    //simple backface cull
                    if (Vector3.Dot(ray.Origin, normal) < 0.0f)
                        continue;

                    result = new GeoShapePickResult(hit.Value, normal, i);
                    return true;
                }
            }

            result = default;
            return false;
        }
    }

    internal readonly record struct GeoPickResult(Vector3 Position, Vector3 Normal, GeoBrush? HitBrush, int FaceIndex);
    internal readonly record struct GeoShapePickResult(Vector3 Position, Vector3 Normal, int FaceIndex);
}
