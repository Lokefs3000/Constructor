using Primary.RHI;
using Primary.RHI;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Rendering
{
    public sealed class RenderPrimitives : IDisposable
    {
        private Lazy<PrimitiveBuffers> _boxPrimitive;

        private bool _disposedValue;

        internal RenderPrimitives()
        {
            _boxPrimitive = new Lazy<PrimitiveBuffers>(CreateBoxPrimitive, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_boxPrimitive.IsValueCreated)
                        _boxPrimitive.Value.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private PrimitiveBuffers CreateBoxPrimitive()
        {
            GenericVertex[] vertices = new GenericVertex[4 * 6];
            ushort[] indices = new ushort[6 * 6];

            {
                int idxOffset = 0;
                int vtxOffset = 0;

                AddFace(Vector3.Zero, Vector3.UnitX, Vector3.UnitY, -Vector3.UnitX);
                AddFace(Vector3.UnitX, Vector3.UnitX, Vector3.UnitY, Vector3.UnitX);

                AddFace(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY, -Vector3.UnitZ);
                AddFace(Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitY, Vector3.UnitZ);

                AddFace(Vector3.Zero, Vector3.UnitX, Vector3.UnitZ, -Vector3.UnitY);
                AddFace(Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY);

                CalculateTangents(vertices, indices);

                void AddFace(Vector3 v, Vector3 x, Vector3 y, Vector3 normal)
                {
                    indices[++idxOffset] = (ushort)(vtxOffset + 1);
                    indices[++idxOffset] = (ushort)(vtxOffset + 2);
                    indices[++idxOffset] = (ushort)vtxOffset;

                    indices[++idxOffset] = (ushort)(vtxOffset + 3);
                    indices[++idxOffset] = (ushort)(vtxOffset + 2);
                    indices[++idxOffset] = (ushort)(vtxOffset + 1);

                    vertices[++vtxOffset] = new GenericVertex(v, normal, Vector3.Zero, Vector3.Zero, Vector2.Zero);
                    vertices[++vtxOffset] = new GenericVertex(v + x, normal, Vector3.Zero, Vector3.Zero, Vector2.UnitX);
                    vertices[++vtxOffset] = new GenericVertex(v + y, normal, Vector3.Zero, Vector3.Zero, Vector2.UnitY);
                    vertices[++vtxOffset] = new GenericVertex(v + (x + y), normal, Vector3.Zero, Vector3.Zero, Vector2.One);
                }
            }

            RHIDevice device = RHIDevice.Instance!;
            
            RHIBuffer vertexBuffer = device.CreateBuffer(new RHIBufferDescription
            {
                Width = (uint)(Unsafe.SizeOf<GenericVertex>() * vertices.Length),
                Stride = Unsafe.SizeOf<GenericVertex>(),
                Usage = RHIResourceUsage.VertexInput
            }, vertices, "BoxPrimitiveVtx")!;

            RHIBuffer indexBuffer = device.CreateBuffer(new RHIBufferDescription
            {
                Width = (uint)(Unsafe.SizeOf<ushort>() * indices.Length),
                Stride = Unsafe.SizeOf<ushort>(),
                Usage = RHIResourceUsage.IndexInput
            }, indices, "BoxPrimitiveIdx")!;

            return new PrimitiveBuffers(vertexBuffer, indexBuffer);
        }

        private void CalculateTangents(Span<GenericVertex> vertices, Span<ushort> indices)
        {
            TangentVector[] tangents = new TangentVector[vertices.Length];

            for (int i = 0; i < indices.Length; i += 3)
            {
                ref GenericVertex v0 = ref vertices[indices[i]];
                ref GenericVertex v1 = ref vertices[indices[i + 1]];
                ref GenericVertex v2 = ref vertices[indices[i + 2]];

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

                ref TangentVector vector0 = ref tangents[indices[i]];
                ref TangentVector vector1 = ref tangents[indices[i + 1]];
                ref TangentVector vector2 = ref tangents[indices[i + 2]];

                vector0.Tangent0 += sdir;
                vector1.Tangent0 += sdir;
                vector2.Tangent0 += sdir;

                vector0.Tangent1 += tdir;
                vector1.Tangent1 += tdir;
                vector2.Tangent1 += tdir;
            }

            for (int i = 0; i < vertices.Length; ++i)
            {
                ref GenericVertex vertex = ref vertices[i];
                ref TangentVector vector = ref tangents[i];

                Vector3 t = Vector3.Normalize(vector.Tangent0 - vertex.Normal * Vector3.Dot(vertex.Normal, vector.Tangent0));

                Vector3 crossT = Vector3.Cross(vertex.Normal, t);
                Vector3 b = Vector3.Normalize(crossT * (Vector3.Dot(crossT, vector.Tangent1) < 0.0f ? -1.0f : 1.0f));

                vertex = new GenericVertex(vertex.Position, vertex.Normal, t, b, vertex.UV);
            }
        }

        public static PrimitiveBuffers Box => Instance._boxPrimitive.Value;

        internal static RenderPrimitives Instance => Engine.GlobalSingleton.RenderingManager.RenderPrimitives;

        private record struct TangentVector(Vector3 Tangent0, Vector3 Tangent1)
        {
            public static readonly TangentVector Zero = new TangentVector(Vector3.Zero, Vector3.Zero);
        }
    }

    public readonly record struct PrimitiveBuffers(RHIBuffer Vertices, RHIBuffer Indices)
    {
        internal void Dispose()
        {
            Vertices.Dispose();
            Indices.Dispose();
        }
    }

    public readonly record struct GenericVertex(Vector3 Position, Vector3 Normal, Vector3 Tangent, Vector3 Bitangent, Vector2 UV);
}
