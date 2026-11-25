using CommunityToolkit.HighPerformance;
using Primary.Collections;
using Primary.Common;
using Primary.Components;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace Editor.Geometry.Mesh
{
    public sealed class BrushMeshGenerator
    {
        private BrushVertex[] _vertices;
        private MeshFace[] _faces;

        private Vector3[] _vertexCache;
        private TangentVector[] _tangentCache;

        public BrushMeshGenerator()
        {
            _vertices = new BrushVertex[6 * 6];
            _faces = new MeshFace[6];

            _vertexCache = new Vector3[8];
            _tangentCache = new TangentVector[3 * 12];
        }

        public BrushMesh UpdateBrushMesh(Brush brush, BrushMesh? oldMesh)
        {
            Matrix4x4 transform = brush.Transform.Model;

            Span<Vector3> brushVertices = brush.Vertices;
            int existingVertices = 0;

            BrushMesh mesh;
            if (oldMesh.HasValue)
            {
                BrushMesh oldMeshValue = oldMesh.Value;

                int validFaces = 0;
                int faceRemap = oldMeshValue.FaceRemap;

                BrushUpdateFlags updateFlags = brush.UpdateFlags;

                if (updateFlags == BrushUpdateFlags.None)
                    return oldMeshValue;

                int vertexOffset = 0;
                int faceOffset = 0;

                foreach (MeshFace face in oldMeshValue.Faces)
                {
                    int[] faceIndices = BrushFaceVertices[(int)face.FaceIndex];

                    int faceShift1 = 1 << faceIndices[0];
                    int faceShift2 = 1 << faceIndices[1];
                    int faceShift3 = 1 << faceIndices[2];
                    int faceShift4 = 1 << faceIndices[3];

                    int totalFaceValue = faceShift1 | faceShift2 | faceShift3 | faceShift4;

                    BrushFace brushFace = brush.Faces[(int)face.FaceIndex];
                    if (!Flags.HasEither(updateFlags, (BrushUpdateFlags)totalFaceValue) && !Flags.HasFlag(brushFace.Flags, BrushFaceFlags.Invisible))
                    {
                        validFaces |= 1 << (int)face.FaceIndex;
                        existingVertices |= totalFaceValue;

                        _vertexCache[faceIndices[0]] = oldMeshValue.Vertices[face.I0].Position;
                        _vertexCache[faceIndices[1]] = oldMeshValue.Vertices[face.I1].Position;
                        _vertexCache[faceIndices[2]] = oldMeshValue.Vertices[face.I2].Position;
                        _vertexCache[faceIndices[3]] = oldMeshValue.Vertices[face.I3].Position;

                        _vertices[vertexOffset++] = oldMeshValue.Vertices[face.I0];
                        _vertices[vertexOffset++] = oldMeshValue.Vertices[face.I1];
                        _vertices[vertexOffset++] = oldMeshValue.Vertices[face.I2];
                        _vertices[vertexOffset++] = oldMeshValue.Vertices[face.I3];
                        _vertices[vertexOffset++] = oldMeshValue.Vertices[face.I4];
                        _vertices[vertexOffset++] = oldMeshValue.Vertices[face.I5];

                        _faces[faceOffset++] = face;
                    }
                    else
                        faceRemap |= 0b111 << ((int)face.FaceIndex * 3);

                    if (existingVertices == 0b11111111)
                        break;
                }

                int previousValidFaces = validFaces;

                BrushFaceIndex faceIndex = 0;
                foreach (BrushFace face in brush.Faces)
                {
                    if (Flags.HasFlag(validFaces, 1 << (int)faceIndex))
                        continue;

                    if (!Flags.HasFlag(face.Flags, BrushFaceFlags.Invisible))
                    {
                        CalculatePlane(
                            brushVertices,
                            face,
                            faceIndex,
                            transform,
                            vertexOffset,
                            ref existingVertices,
                            _vertices.AsSpan(vertexOffset, 6),
                            ref _faces[faceOffset]);

                        int shift = (int)faceIndex * 3;
                        faceRemap = (faceRemap & ~(0b111 << shift)) | ((int)faceIndex << shift);
                        validFaces |= 1 << (int)faceIndex;

                        vertexOffset += 6;
                        ++faceOffset;

                    }

                    ++faceIndex;
                }

                if (previousValidFaces == validFaces)
                    return oldMeshValue;
                else
                    mesh = new BrushMesh(brush.UpdateIndex, faceRemap, [.. _vertices.AsSpan(0, vertexOffset)], [.. _faces.AsSpan(0, faceOffset)]);
            }
            else
            {
                int vertexOffset = 0;
                int faceOffset = 0;

                int faceRemap = EmptyFaceRemap;

                BrushFaceIndex faceIndex = 0;
                foreach (BrushFace face in brush.Faces)
                {
                    if (!Flags.HasFlag(face.Flags, BrushFaceFlags.Invisible))
                    {
                        CalculatePlane(
                            brushVertices,
                            face,
                            faceIndex,
                            transform,
                            vertexOffset,
                            ref existingVertices,
                            _vertices.AsSpan(vertexOffset, 6),
                            ref _faces[faceOffset]);

                        int shift = (int)faceIndex * 3;
                        faceRemap = (faceRemap & ~(0b111 << shift)) | ((int)faceIndex << shift);

                        vertexOffset += 6;
                        ++faceOffset;
                    }

                    ++faceIndex;
                }

                mesh = new BrushMesh(brush.UpdateIndex, faceRemap, [.. _vertices.AsSpan(0, vertexOffset)], [.. _faces.AsSpan(0, faceOffset)]);
            }

            if (mesh.Faces.Length > 0)
                CalculateTangents(mesh.Vertices, mesh.Faces);
            return mesh;
        }

        private void CalculatePlane(Span<Vector3> inVertices, BrushFace face, BrushFaceIndex faceIndex, Matrix4x4 transform, int baseVertex, ref int existingVertices, Span<BrushVertex> outVertices, ref MeshFace outFace)
        {
            int[] faceIndices = BrushFaceVertices[(int)faceIndex];

            int faceShift1 = 1 << faceIndices[0];
            int faceShift2 = 1 << faceIndices[1];
            int faceShift3 = 1 << faceIndices[2];
            int faceShift4 = 1 << faceIndices[3];

            // triangle 1: v0 -> v2 -> v3
            // triangle 2: v0 -> v1 -> v3

            Vector3 v0 = Flags.HasFlag(existingVertices, faceShift1) ? _vertexCache[faceIndices[0]] : Vector3.Transform(inVertices.DangerousGetReferenceAt(faceIndices[0]), transform);
            Vector3 v1 = Flags.HasFlag(existingVertices, faceShift2) ? _vertexCache[faceIndices[1]] : Vector3.Transform(inVertices.DangerousGetReferenceAt(faceIndices[1]), transform);
            Vector3 v2 = Flags.HasFlag(existingVertices, faceShift3) ? _vertexCache[faceIndices[2]] : Vector3.Transform(inVertices.DangerousGetReferenceAt(faceIndices[2]), transform);
            Vector3 v3 = Flags.HasFlag(existingVertices, faceShift4) ? _vertexCache[faceIndices[3]] : Vector3.Transform(inVertices.DangerousGetReferenceAt(faceIndices[3]), transform);

            Vector3 edge20 = v2 - v0;
            Vector3 edge30 = v3 - v0;

            Vector3 edge13 = v1 - v3;
            Vector3 edge03 = v0 - v3;

            Vector3 normal0 = Vector3.Normalize(Vector3.Cross(edge20, edge30));
            Vector3 normal1 = Vector3.Normalize(Vector3.Cross(edge13, edge03));

            int highest;
            {
                Vector128<float> comp = Vector128.GreaterThan(normal0.AsVector128(), normal1.AsVector128());
                int sum = (int)Vector128.Sum(comp);

                highest = sum >= 2 ? 0 : 1;
            }

            Vector4 uv01;
            Vector4 uv23;
            {
                Vector3 normal = highest == 0 ? normal0 : normal1;

                Vector3 axis;
                switch (faceIndex)
                {
                    case BrushFaceIndex.XPlus:
                    case BrushFaceIndex.XNegative: axis = Vector3.UnitY; break;
                    default: axis = Vector3.UnitZ; break;
                }

                Vector3 u = Vector3.Cross(normal, axis);
                if (Vector3.Dot(u, u) < 0.001f)
                    u = Vector3.UnitX;
                else
                    u = Vector3.Normalize(u);

                Vector3 v = Vector3.Normalize(Vector3.Cross(normal, u));

                Vector3 uv0_p = v0 - (v0 * normal);
                Vector3 uv1_p = v1 - (v1 * normal);
                Vector3 uv2_p = v2 - (v2 * normal);
                Vector3 uv3_p = v3 - (v3 * normal);

                Vector4 scale = Vector4.One / new Vector4(face.UVScale.X, face.UVScale.Y, face.UVScale.X, face.UVScale.Y);

                if (faceIndex == BrushFaceIndex.ZNegative)
                {
                    scale.Y = -scale.Y;
                    scale.W = -scale.W;
                }

                uv01 = new Vector4(Vector3.Dot(uv0_p, u), Vector3.Dot(uv0_p, v), Vector3.Dot(uv1_p, u), Vector3.Dot(uv1_p, v)) * scale;
                uv23 = new Vector4(Vector3.Dot(uv2_p, u), Vector3.Dot(uv2_p, v), Vector3.Dot(uv3_p, u), Vector3.Dot(uv3_p, v)) * scale;

                Vector4 uvMin = Vector4.Min(uv01, uv23);
                Vector2 absMin = Vector2.Min(
                    Unsafe.ReadUnaligned<Vector2>(ref Unsafe.As<Vector4, byte>(ref uvMin)),
                    Unsafe.ReadUnaligned<Vector2>(ref Unsafe.Add(ref Unsafe.As<Vector4, byte>(ref uvMin), 8)));
                uvMin = Vector4.Truncate(new Vector4(absMin.X, absMin.Y, absMin.X, absMin.Y) / scale) * scale;

                uv01 -= uvMin;
                uv23 -= uvMin;
            }

            Vector128<ushort> vertexIndices = Vector128.Create((ushort)baseVertex) + Vector128<ushort>.Indices;

            outVertices[0] = new BrushVertex(v0, normal0, Vector3.Zero, Vector3.Zero, new Vector2(uv01.X, uv01.Y));
            outVertices[1] = new BrushVertex(v2, normal0, Vector3.Zero, Vector3.Zero, new Vector2(uv23.X, uv23.Y));
            outVertices[2] = new BrushVertex(v3, normal0, Vector3.Zero, Vector3.Zero, new Vector2(uv23.Z, uv23.W));

            outVertices[3] = new BrushVertex(v3, normal1, Vector3.Zero, Vector3.Zero, new Vector2(uv23.Z, uv23.W));
            outVertices[4] = new BrushVertex(v1, normal1, Vector3.Zero, Vector3.Zero, new Vector2(uv01.Z, uv01.W));
            outVertices[5] = new BrushVertex(v0, normal1, Vector3.Zero, Vector3.Zero, new Vector2(uv01.X, uv01.Y));

            outFace = new MeshFace(faceIndex, face.Material,
                (byte)vertexIndices[0],
                (byte)vertexIndices[1],
                (byte)vertexIndices[2],
                (byte)vertexIndices[3],
                (byte)vertexIndices[4],
                (byte)vertexIndices[5]);

            if (!Flags.HasFlag(existingVertices, faceShift1))
                _vertexCache[faceIndices[0]] = v0;
            if (!Flags.HasFlag(existingVertices, faceShift2))
                _vertexCache[faceIndices[1]] = v1;
            if (!Flags.HasFlag(existingVertices, faceShift3))
                _vertexCache[faceIndices[2]] = v2;
            if (!Flags.HasFlag(existingVertices, faceShift4))
                _vertexCache[faceIndices[3]] = v3;

            existingVertices = Flags.AddFlags(existingVertices, faceShift1 | faceShift2 | faceShift3 | faceShift4);
        }

        private void CalculateTangents(Span<BrushVertex> vertices, Span<MeshFace> faces)
        {
            _tangentCache.AsSpan(0, vertices.Length).Fill(TangentVector.Zero);

            for (int i = 0; i < faces.Length; ++i)
            {
                ref MeshFace face = ref faces[i];

                ContributeTangent(face.I0, face.I1, face.I2, vertices);
                ContributeTangent(face.I3, face.I4, face.I5, vertices);
            }

            for (int i = 0; i < vertices.Length; ++i)
            {
                ref BrushVertex vertex = ref vertices[i];
                TangentVector tangent = _tangentCache[i];

                Vector3 t = Vector3.Normalize(tangent.Tangent0 - vertex.Normal * Vector3.Dot(vertex.Normal, tangent.Tangent0));
                Vector3 b = Vector3.Normalize(Vector3.Cross(vertex.Normal, t));

                vertex = new BrushVertex(vertex.Position, vertex.Normal, t, b, vertex.UV);
            }
        }

        private void ContributeTangent(byte i0, byte i1, byte i2, Span<BrushVertex> vertices)
        {
            ref BrushVertex v0 = ref vertices[i0];
            ref BrushVertex v1 = ref vertices[i1];
            ref BrushVertex v2 = ref vertices[i2];

            Vector3 p0 = v1.Position - v0.Position;
            Vector3 p1 = v2.Position - v0.Position;

            Vector2 s = new Vector2(v1.UV.X, v2.UV.X) - new Vector2(v0.UV.X);
            Vector2 t = new Vector2(v1.UV.Y, v2.UV.Y) - new Vector2(v0.UV.Y);

            float r = 1.0f / Vector2.Cross(s, t);
            Vector3 sdir = new Vector3(
                Vector2.Cross(t, new Vector2(p0.X, p1.X)),
                Vector2.Cross(t, new Vector2(p0.Y, p1.Y)),
                Vector2.Cross(t, new Vector2(p0.Z, p1.Z))) * r;

            Vector3 tdir = new Vector3(
                Vector2.Cross(s, new Vector2(p0.X, p1.X)),
                Vector2.Cross(s, new Vector2(p0.Y, p1.Y)),
                Vector2.Cross(s, new Vector2(p0.Z, p1.Z))) * r;

            ref TangentVector vector0 = ref _tangentCache[i0];
            ref TangentVector vector1 = ref _tangentCache[i1];
            ref TangentVector vector2 = ref _tangentCache[i2];

            vector0.Tangent0 += sdir;
            vector1.Tangent0 += sdir;
            vector2.Tangent0 += sdir;

            vector0.Tangent1 += tdir;
            vector1.Tangent1 += tdir;
            vector2.Tangent1 += tdir;
        }

        public static readonly int[][] BrushFaceVertices = [
            [ (int)BrushVertexIndex.BackTopRight, (int)BrushVertexIndex.FrontTopRight, (int)BrushVertexIndex.BackBottomRight, (int)BrushVertexIndex.FrontBottomRight ],     //X+
            [ (int)BrushVertexIndex.BackBottomLeft, (int)BrushVertexIndex.FrontBottomLeft, (int)BrushVertexIndex.BackTopLeft, (int)BrushVertexIndex.FrontTopLeft ],         //X-
            [ (int)BrushVertexIndex.FrontTopLeft, (int)BrushVertexIndex.FrontTopRight, (int)BrushVertexIndex.BackTopLeft, (int)BrushVertexIndex.BackTopRight ],             //Y+
            [ (int)BrushVertexIndex.BackBottomLeft, (int)BrushVertexIndex.BackBottomRight, (int)BrushVertexIndex.FrontBottomLeft, (int)BrushVertexIndex.FrontBottomRight ], //Y-
            [ (int)BrushVertexIndex.FrontBottomLeft, (int)BrushVertexIndex.FrontBottomRight, (int)BrushVertexIndex.FrontTopLeft, (int)BrushVertexIndex.FrontTopRight ],     //Z+
            [ (int)BrushVertexIndex.BackTopLeft, (int)BrushVertexIndex.BackTopRight, (int)BrushVertexIndex.BackBottomLeft, (int)BrushVertexIndex.BackBottomRight ],         //Z-
            ];

        public static readonly ImmutableArray<ImmutableArray<int>> BrushFaceTriangles =
            [.. BrushFaceVertices.Select(static (x) => {
                return new int[] { x[0], x[2], x[3], x[0], x[1], x[3] }.ToImmutableArray();
            })];

        public static readonly int[] BrushFaceMasks =
            BrushFaceVertices.Select(static (x) =>
            {
                int mask = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    mask |= 1 << x[i];
                }

                return mask;
            }).ToArray();

        public const int EmptyFaceRemap = 0x3ffff;

        private static readonly Quaternion s_basisRotation = Quaternion.CreateFromYawPitchRoll(MathF.PI * -0.5f, MathF.PI * -0.5f, MathF.PI * -0.5f);

        private static readonly Quaternion s_basisRotationU = Quaternion.CreateFromYawPitchRoll(0.0f, MathF.PI, 0.0f);
        private static readonly Quaternion s_basisRotationV = Quaternion.CreateFromYawPitchRoll(0.0f, 0.0f, MathF.PI);

        private record struct TangentVector(Vector3 Tangent0, Vector3 Tangent1)
        {
            public static readonly TangentVector Zero = new TangentVector(Vector3.Zero, Vector3.Zero);
        }
    }
}
