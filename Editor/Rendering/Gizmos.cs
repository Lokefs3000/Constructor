using Arch.LowLevel;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Rendering
{
    public sealed class Gizmos : IDisposable
    {
        private readonly static WeakReference s_instance = new WeakReference(null);

        private List<GizmoDrawCommand> _commands;
        private UnsafeList<GZLineVertex> _lineVertices;
        private UnsafeList<GZSphereData> _sphereData;
        private UnsafeList<GZCubeData> _cubeData;

        private int _counter;

        private bool _disposedValue;

        internal Gizmos()
        {
            s_instance.Target = this;

            _commands = new List<GizmoDrawCommand>();
            _lineVertices = new UnsafeList<GZLineVertex>(8);
            _sphereData = new UnsafeList<GZSphereData>(8);
            _cubeData = new UnsafeList<GZCubeData>(8);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    s_instance.Target = null;
                }

                _lineVertices.Dispose();
                _sphereData.Dispose();
                _cubeData.Dispose();

                _disposedValue = true;
            }
        }

        ~Gizmos()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ResetForNewFrame()
        {
            _commands.Clear();
            _lineVertices.Clear();
            _sphereData.Clear();
            _cubeData.Clear();

            _counter = 0;

            DrawLine(Vector3.Zero, Vector3.UnitX, new Vector4(1.0f, 0.0f, 0.0f, 0.5f));
            DrawLine(Vector3.Zero, Vector3.UnitY, new Vector4(0.0f, 1.0f, 0.0f, 0.5f));
            DrawLine(Vector3.Zero, Vector3.UnitZ, new Vector4(0.0f, 0.0f, 1.0f, 0.5f));
        }

        internal void FinalizeBuffers()
        {
            if (_commands.Count > 0)
            {
                ref GizmoDrawCommand last = ref _commands.AsSpan()[_commands.Count - 1];
                last.ElementCount = _counter;
            }
        }

        private void EnsureCommand(GizmoPolyType polyType, int elemOffset)
        {
            bool r = _commands.Count == 0;
            if (!r)
            {
                r = _commands[_commands.Count - 1].PolyType != polyType;
            }

            if (r)
            {
                if (_commands.Count > 0)
                {
                    ref GizmoDrawCommand last = ref _commands.AsSpan()[_commands.Count - 1];
                    last.ElementCount = _counter;
                }

                _commands.Add(new GizmoDrawCommand(polyType));
            }
        }

        public static void DrawCube(Vector3 center, Vector3 size, Vector4 color)
        {
            Gizmos @this = Instance;
            @this.EnsureCommand(GizmoPolyType.Cube, @this._cubeData.Count);
            @this._cubeData.Add(new GZCubeData(center, size, color));
        }
        public static void DrawCube(AABB aabb, Vector4 color) => DrawCube(aabb.Center, aabb.Size, color);

        public static void DrawFrustrum(Vector3 center, Vector3 forward, float fov, float near, float far, float aspect, Vector4 color)
        {
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, far);
            Matrix4x4 view = Matrix4x4.CreateLookTo(center, forward, Vector3.UnitY);

            Matrix4x4.Invert(projection, out projection);
            Matrix4x4.Invert(view, out view);

            Matrix4x4 viewProjection = view * projection;

            Vector3 bottomBackLeft = Vector3.Transform(new Vector3(-1.0f, -1.0f, -1.0f), viewProjection);
            Vector3 bottomBackRight = Vector3.Transform(new Vector3(1.0f, -1.0f, -1.0f), viewProjection);
            Vector3 bottomFrontLeft = Vector3.Transform(new Vector3(-1.0f, -1.0f, 1.0f), viewProjection);
            Vector3 bottomFrontRight = Vector3.Transform(new Vector3(1.0f, -1.0f, 1.0f), viewProjection);

            Vector3 topBackLeft = Vector3.Transform(new Vector3(-1.0f, 1.0f, -1.0f), viewProjection);
            Vector3 topBackRight = Vector3.Transform(new Vector3(1.0f, 1.0f, -1.0f), viewProjection);
            Vector3 topFrontLeft = Vector3.Transform(new Vector3(-1.0f, 1.0f, 1.0f), viewProjection);
            Vector3 topFrontRight = Vector3.Transform(new Vector3(1.0f, 1.0f, 1.0f), viewProjection);
        }

        public static void DrawLine(Vector3 from, Vector3 to, Vector4 color)
        {
            Gizmos @this = Instance;
            @this.EnsureCommand(GizmoPolyType.Line, @this._lineVertices.Count);
            @this._lineVertices.Add(new GZLineVertex(from, color));
            @this._lineVertices.Add(new GZLineVertex(to, color));
            @this._counter += 2;
        }

        public static void DrawRay(Ray ray, float length, Vector4 color)
        {
            Gizmos @this = Instance;
            @this.EnsureCommand(GizmoPolyType.Line, @this._lineVertices.Count);
            @this._lineVertices.Add(new GZLineVertex(ray.Origin, color));
            @this._lineVertices.Add(new GZLineVertex(ray.AtDistance(length), color));
            @this._counter += 2;
        }

        public static void DrawSphere(Vector3 center, float radius, Vector4 color)
        {
            Gizmos @this = Instance;
            @this.EnsureCommand(GizmoPolyType.Sphere, @this._sphereData.Count);
            @this._sphereData.Add(new GZSphereData(center, radius, color, false));
            @this._counter += 1;
        }

        public static void DrawWireCube(Vector3 center, Vector3 size, Vector4 color)
        {
            Gizmos @this = Instance;
            @this.EnsureCommand(GizmoPolyType.Line, @this._lineVertices.Count);

            Vector3 halfSize = size * 0.5f;
            Vector3 min = center - size;
            Vector3 max = center + size;

            @this._lineVertices.Add(new GZLineVertex(new Vector3(min.X, min.Y, min.Z), color));
            @this._lineVertices.Add(new GZLineVertex(new Vector3(min.X, min.Y, max.Z), color));
            @this._lineVertices.Add(new GZLineVertex(new Vector3(max.X, min.Y, min.Z), color));
            @this._lineVertices.Add(new GZLineVertex(new Vector3(max.X, min.Y, max.Z), color));

            @this._lineVertices.Add(new GZLineVertex(new Vector3(min.X, max.Y, min.Z), color));
            @this._lineVertices.Add(new GZLineVertex(new Vector3(min.X, max.Y, max.Z), color));
            @this._lineVertices.Add(new GZLineVertex(new Vector3(max.X, max.Y, min.Z), color));
            @this._lineVertices.Add(new GZLineVertex(new Vector3(max.X, max.Y, max.Z), color));

            @this._lineVertices.Add(new GZLineVertex(new Vector3(min.X, min.Y, min.Z), color));
            @this._lineVertices.Add(new GZLineVertex(new Vector3(min.X, max.Y, min.Z), color));
            @this._lineVertices.Add(new GZLineVertex(new Vector3(max.X, min.Y, min.Z), color));
            @this._lineVertices.Add(new GZLineVertex(new Vector3(max.X, max.Y, min.Z), color));

            @this._lineVertices.Add(new GZLineVertex(new Vector3(min.X, min.Y, max.Z), color));
            @this._lineVertices.Add(new GZLineVertex(new Vector3(min.X, max.Y, max.Z), color));
            @this._lineVertices.Add(new GZLineVertex(new Vector3(max.X, min.Y, max.Z), color));
            @this._lineVertices.Add(new GZLineVertex(new Vector3(max.X, max.Y, max.Z), color));

            @this._counter += 16;
        }
        public static void DrawWireCube(AABB aabb, Vector4 color) => DrawWireCube(aabb.Center, aabb.Size, color);

        public static void DrawWireSphere(Vector3 center, float radius, int subDivisions, Vector4 color)
        {
            Gizmos @this = Instance;
            @this.EnsureCommand(GizmoPolyType.Line, @this._lineVertices.Count);

            Vector3 prevFrom1 = center + new Vector3(0.0f, 0.0f, radius);
            Vector3 prevFrom2 = center + new Vector3(radius, 0.0f, 0.0f);
            Vector3 prevFrom3 = center + new Vector3(0.0f, 0.0f, radius);

            for (int i = 0; i < subDivisions; i++)
            {
                (float sin, float cos) = MathF.SinCos((i + 1) / (float)subDivisions * MathF.PI * 2.0f);

                sin *= radius;
                cos *= radius;
                
                Vector3 to1 = center + new Vector3(0.0f, sin, cos);
                Vector3 to2 = center + new Vector3(cos, sin, 0.0f);
                Vector3 to3 = center + new Vector3(sin, 0.0f, cos);

                @this._lineVertices.Add(new GZLineVertex(prevFrom1, color));
                @this._lineVertices.Add(new GZLineVertex(to1, color));

                @this._lineVertices.Add(new GZLineVertex(prevFrom2, color));
                @this._lineVertices.Add(new GZLineVertex(to2, color));

                @this._lineVertices.Add(new GZLineVertex(prevFrom3, color));
                @this._lineVertices.Add(new GZLineVertex(to3, color));

                prevFrom1 = to1;
                prevFrom2 = to2;
                prevFrom3 = to3;
            }

            @this._counter += subDivisions * 6;
        }

        internal Span<GZLineVertex> Vertices => _lineVertices.AsSpan();
        internal Span<GZSphereData> Spheres => _sphereData.AsSpan();
        internal Span<GZCubeData> Cubes => _cubeData.AsSpan();

        internal Span<GizmoDrawCommand> DrawCommands => _commands.AsSpan();

        internal bool IsEmpty => _commands.Count == 0;

        internal static Gizmos Instance => NullableUtility.ThrowIfNull(Unsafe.As<Gizmos>(s_instance.Target));
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct GizmoDrawCommand
    {
        [FieldOffset(0)]
        internal readonly GizmoPolyType PolyType;
        [FieldOffset(1)]
        internal int ElementCount;

        internal GizmoDrawCommand(GizmoPolyType polyType)
        {
            PolyType = polyType;
            ElementCount = 0;
        }
    }

    internal enum GizmoPolyType : byte
    {
        Line = 0,
        Cube,
        Sphere,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct GZLineVertex
    {
        internal readonly Vector3 Position;
        internal readonly Vector4 Color;

        internal GZLineVertex(Vector3 position, Vector4 color)
        {
            Position = position;
            Color = color;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct GZCubeData
    {
        internal readonly Vector3 Center;
        internal readonly Vector3 Size;
        internal readonly Vector4 Color;

        internal GZCubeData(Vector3 center, Vector3 size, Vector4 color)
        {
            Center = center;
            Size = size;
            Color = color;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct GZSphereData
    {
        internal readonly Vector3 Center;
        internal readonly float Radius;
        internal readonly Vector4 Color;
        internal readonly uint IsWire;

        internal GZSphereData(Vector3 center, float radius, Vector4 color, bool isWire)
        {
            Center = center;
            Radius = radius;
            Color = color;
            IsWire = isWire ? 1u : 0;
        }
    }
}
