using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Mathematics;

namespace Primary.Rendering.Diagnostics
{
    public sealed class Gizmos : IDisposable
    {
        private static readonly WeakReference s_instance = new WeakReference(null);

        private List<GizmoSection> _sections;

        private List<GizmoVertex> _vertices;
        private List<uint> _indices;

        private Color _primaryColor;

        private GizmosMode _currentMode;

        internal Gizmos()
        {
            s_instance.Target = this;

            _sections = new List<GizmoSection>();

            _vertices = new List<GizmoVertex>();
            _indices = new List<uint>();

            _primaryColor = Color.White;

            _currentMode = GizmosMode.Undefined;
        }

        public void Dispose()
        {
            if (s_instance.Target == this)
                s_instance.Target = null;
        }

        internal void ClearDrawData()
        {
            _sections.Clear();

            _vertices.Clear();
            _indices.Clear();

            _primaryColor = Color.White;

            _currentMode = GizmosMode.Undefined;
        }

        private void CheckCurrentSection(GizmosMode mode)
        {
            if (_currentMode != mode)
            {
                _sections.Add(new GizmoSection(_indices.Count, mode));
                _currentMode = mode;
            }
        }

        #region Interface
        public static void DrawLine(Vector3 from, Vector3 to, Color? color = null)
        {
            Gizmos self = Instance;
            self.CheckCurrentSection(GizmosMode.Wire);

            Color c = color.GetValueOrDefault(self._primaryColor);

            self._indices.Add((uint)self._vertices.Count);
            self._indices.Add((uint)(self._vertices.Count + 1));

            self._vertices.Add(new GizmoVertex(from, c));
            self._vertices.Add(new GizmoVertex(to, c));
        }

        public static void DrawVector(Vector3 position)
        {
            Gizmos self = Instance;
            self.CheckCurrentSection(GizmosMode.Wire);

            self._indices.Add((uint)self._vertices.Count);
            self._indices.Add((uint)(self._vertices.Count + 3));
            self._indices.Add((uint)(self._vertices.Count + 1));
            self._indices.Add((uint)(self._vertices.Count + 4));
            self._indices.Add((uint)(self._vertices.Count + 2));
            self._indices.Add((uint)(self._vertices.Count + 5));

            self._vertices.Add(new GizmoVertex(position, Color.Red));
            self._vertices.Add(new GizmoVertex(position, Color.Green));
            self._vertices.Add(new GizmoVertex(position, Color.Blue));

            self._vertices.Add(new GizmoVertex(position + Vector3.UnitX, Color.Red));
            self._vertices.Add(new GizmoVertex(position + Vector3.UnitY, Color.Green));
            self._vertices.Add(new GizmoVertex(position + Vector3.UnitZ, Color.Blue));
        }

        public static void DrawWireTriangle(Vector3 pointA, Vector3 pointB, Vector3 pointC, Color? color = null)
        {
            Gizmos self = Instance;
            self.CheckCurrentSection(GizmosMode.Wire);

            Color c = color.GetValueOrDefault(self._primaryColor);

            uint i0 = (uint)self._vertices.Count;
            uint i1 = (uint)(self._vertices.Count + 1);
            uint i2 = (uint)(self._vertices.Count + 2);

            self._indices.Add(i0);
            self._indices.Add(i1);

            self._indices.Add(i1);
            self._indices.Add(i2);

            self._indices.Add(i2);
            self._indices.Add(i0);

            self._vertices.Add(new GizmoVertex(pointA, c));
            self._vertices.Add(new GizmoVertex(pointB, c));
            self._vertices.Add(new GizmoVertex(pointC, c));
        }

        public static void DrawWireSphere(Vector3 center, float radius, Color? color = null)
        {
            Gizmos self = Instance;
            self.CheckCurrentSection(GizmosMode.Wire);

            Color c = color.GetValueOrDefault(self._primaryColor);

            const int Steps = 12;
            const float StepMult = float.Pi * 2.0f / Steps;

            for (int i = 0; i <= Steps; ++i)
            {
                (float sin, float cos) = MathF.SinCos(i * StepMult);

                Vector2 sinCos = new Vector2(sin, cos) * radius;

                Vector3 positionX = center + new Vector3(0.0f, sinCos.X, sinCos.Y);
                Vector3 positionY = center + new Vector3(sinCos.X, 0.0f, sinCos.Y);
                Vector3 positionZ = center + new Vector3(sinCos.X, sinCos.Y, 0.0f);

                if (i > 0)
                {
                    self._indices.Add((uint)(self._vertices.Count - 3));
                    self._indices.Add((uint)self._vertices.Count);

                    self._indices.Add((uint)(self._vertices.Count - 2));
                    self._indices.Add((uint)(self._vertices.Count + 1));

                    self._indices.Add((uint)(self._vertices.Count - 1));
                    self._indices.Add((uint)(self._vertices.Count + 2));
                }

                self._vertices.Add(new GizmoVertex(positionX, c));
                self._vertices.Add(new GizmoVertex(positionY, c));
                self._vertices.Add(new GizmoVertex(positionZ, c));
            }
        }

        public static void DrawWireAABB(AABB aabb, Color? color = null)
        {
            Gizmos self = Instance;
            self.CheckCurrentSection(GizmosMode.Wire);

            Color c = color.GetValueOrDefault(self._primaryColor);

            uint i0 = (uint)self._vertices.Count;
            uint i1 = (uint)(self._vertices.Count + 1);
            uint i2 = (uint)(self._vertices.Count + 2);
            uint i3 = (uint)(self._vertices.Count + 3);
            uint i4 = (uint)(self._vertices.Count + 4);
            uint i5 = (uint)(self._vertices.Count + 5);
            uint i6 = (uint)(self._vertices.Count + 6);
            uint i7 = (uint)(self._vertices.Count + 7);

            self._indices.Add(i0);
            self._indices.Add(i5);
            self._indices.Add(i1);
            self._indices.Add(i4);

            self._indices.Add(i0);
            self._indices.Add(i1);
            self._indices.Add(i5);
            self._indices.Add(i4);

            self._indices.Add(i3);
            self._indices.Add(i6);
            self._indices.Add(i2);
            self._indices.Add(i7);

            self._indices.Add(i3);
            self._indices.Add(i2);
            self._indices.Add(i6);
            self._indices.Add(i7);

            self._indices.Add(i0);
            self._indices.Add(i3);
            self._indices.Add(i1);
            self._indices.Add(i2);

            self._indices.Add(i5);
            self._indices.Add(i6);
            self._indices.Add(i4);
            self._indices.Add(i7);

            self._vertices.Add(new GizmoVertex(aabb.Minimum, c));                                                   // ---      0
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Maximum.X, aabb.Minimum.Y, aabb.Minimum.Z), c));    // +--      1
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Maximum.X, aabb.Maximum.Y, aabb.Minimum.Z), c));    // ++-      2
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Minimum.X, aabb.Maximum.Y, aabb.Minimum.Z), c));    // -+-      3
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Maximum.X, aabb.Minimum.Y, aabb.Maximum.Z), c));    // +-+      4
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Minimum.X, aabb.Minimum.Y, aabb.Maximum.Z), c));    // --+      5
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Minimum.X, aabb.Maximum.Y, aabb.Maximum.Z), c));    // -++      6
            self._vertices.Add(new GizmoVertex(aabb.Maximum, c));                                                   // +++      7
        }

        public static void DrawWireBox(Vector3 min, Vector3 max, Color? color = null) => DrawWireAABB(new AABB(min, max), color);

        public static void DrawSolidAABB(AABB aabb, Color? color = null)
        {
            Gizmos self = Instance;
            self.CheckCurrentSection(GizmosMode.Solid);

            Color c = color.GetValueOrDefault(self._primaryColor);

            uint i0 = (uint)self._vertices.Count;
            uint i1 = (uint)(self._vertices.Count + 1);
            uint i2 = (uint)(self._vertices.Count + 2);
            uint i3 = (uint)(self._vertices.Count + 3);
            uint i4 = (uint)(self._vertices.Count + 4);
            uint i5 = (uint)(self._vertices.Count + 5);
            uint i6 = (uint)(self._vertices.Count + 6);
            uint i7 = (uint)(self._vertices.Count + 7);

            self._indices.Add(i1);
            self._indices.Add(i2);
            self._indices.Add(i0);

            self._indices.Add(i0);
            self._indices.Add(i2);
            self._indices.Add(i3);

            self._indices.Add(i6);
            self._indices.Add(i7);
            self._indices.Add(i5);

            self._indices.Add(i4);
            self._indices.Add(i5);
            self._indices.Add(i7);

            self._indices.Add(i6);
            self._indices.Add(i5);
            self._indices.Add(i0);

            self._indices.Add(i6);
            self._indices.Add(i0);
            self._indices.Add(i3);

            self._indices.Add(i1);
            self._indices.Add(i4);
            self._indices.Add(i7);

            self._indices.Add(i2);
            self._indices.Add(i1);
            self._indices.Add(i7);

            self._indices.Add(i7);
            self._indices.Add(i6);
            self._indices.Add(i3);

            self._indices.Add(i2);
            self._indices.Add(i7);
            self._indices.Add(i3);

            self._indices.Add(i0);
            self._indices.Add(i5);
            self._indices.Add(i4);

            self._indices.Add(i4);
            self._indices.Add(i1);
            self._indices.Add(i0);

            self._vertices.Add(new GizmoVertex(aabb.Minimum, c));                                                   // ---      0
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Maximum.X, aabb.Minimum.Y, aabb.Minimum.Z), c));    // +--      1
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Maximum.X, aabb.Maximum.Y, aabb.Minimum.Z), c));    // ++-      2
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Minimum.X, aabb.Maximum.Y, aabb.Minimum.Z), c));    // -+-      3
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Maximum.X, aabb.Minimum.Y, aabb.Maximum.Z), c));    // +-+      4
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Minimum.X, aabb.Minimum.Y, aabb.Maximum.Z), c));    // --+      5
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Minimum.X, aabb.Maximum.Y, aabb.Maximum.Z), c));    // -++      6
            self._vertices.Add(new GizmoVertex(aabb.Maximum, c));                                                   // +++      7
        }

        public static void DrawSolidTriangle(Vector3 pointA, Vector3 pointB, Vector3 pointC, Color? color = null)
        {
            Gizmos self = Instance;
            self.CheckCurrentSection(GizmosMode.Solid);

            Color c = color.GetValueOrDefault(self._primaryColor);

            uint i0 = (uint)self._vertices.Count;
            uint i1 = (uint)(self._vertices.Count + 1);
            uint i2 = (uint)(self._vertices.Count + 2);

            self._indices.Add(i0);
            self._indices.Add(i2);
            self._indices.Add(i1);

            self._vertices.Add(new GizmoVertex(pointA, c));
            self._vertices.Add(new GizmoVertex(pointB, c));
            self._vertices.Add(new GizmoVertex(pointC, c));
        }

        public static void DrawWireCircle(Vector3 center, float radius, Color? color = null)
        {
            Gizmos self = Instance;
            self.CheckCurrentSection(GizmosMode.Wire);

            Color c = color.GetValueOrDefault(self._primaryColor);

            const int Steps = 32;
            const float StepMult = float.Pi * 2.0f / Steps;

            for (int i = 0; i < Steps; i++)
            {
                (float sin, float cos) = MathF.SinCos(i * StepMult);

                Vector2 sinCos = new Vector2(sin, cos) * radius;
                Vector3 position = center + new Vector3(0.0f, sinCos.X, sinCos.Y);

                if (i > 0)
                {
                    self._indices.Add((uint)(self._vertices.Count - 1));
                    self._indices.Add((uint)self._vertices.Count);
                }

                self._vertices.Add(new GizmoVertex(position, c));
            }
        }

        public static void DrawWireRect(Vector2 min, Vector2 max, Color? color = null)
        {
            Gizmos self = Instance;
            self.CheckCurrentSection(GizmosMode.Wire);

            Color c = color.GetValueOrDefault(self._primaryColor);

            uint i0 = (uint)self._vertices.Count;
            uint i1 = (uint)(self._vertices.Count + 1);
            uint i2 = (uint)(self._vertices.Count + 2);
            uint i3 = (uint)(self._vertices.Count + 3);

            self._indices.Add(i0);
            self._indices.Add(i1);

            self._indices.Add(i1);
            self._indices.Add(i2);

            self._indices.Add(i2);
            self._indices.Add(i3);

            self._indices.Add(i3);
            self._indices.Add(i0);

            self._vertices.Add(new GizmoVertex(new Vector3(min.X, min.Y, 0.0f), c));
            self._vertices.Add(new GizmoVertex(new Vector3(max.X, min.Y, 0.0f), c));
            self._vertices.Add(new GizmoVertex(new Vector3(max.X, max.Y, 0.0f), c));
            self._vertices.Add(new GizmoVertex(new Vector3(min.X, max.Y, 0.0f), c));
        }

        public static void DrawSolidRect(Vector2 min, Vector2 max, Color? color = null)
        {
            Gizmos self = Instance;
            self.CheckCurrentSection(GizmosMode.Solid);

            Color c = color.GetValueOrDefault(self._primaryColor);

            uint i0 = (uint)self._vertices.Count;
            uint i1 = (uint)(self._vertices.Count + 1);
            uint i2 = (uint)(self._vertices.Count + 2);
            uint i3 = (uint)(self._vertices.Count + 3);

            self._indices.Add(i0);
            self._indices.Add(i2);
            self._indices.Add(i1);

            self._indices.Add(i1);
            self._indices.Add(i3);
            self._indices.Add(i2);

            self._vertices.Add(new GizmoVertex(new Vector3(min.X, min.Y, 0.0f), c));
            self._vertices.Add(new GizmoVertex(new Vector3(max.X, min.Y, 0.0f), c));
            self._vertices.Add(new GizmoVertex(new Vector3(max.X, max.Y, 0.0f), c));
            self._vertices.Add(new GizmoVertex(new Vector3(min.X, max.Y, 0.0f), c));
        }
        #endregion

        internal Span<GizmoSection> Sections => _sections.AsSpan();

        internal Span<GizmoVertex> Vertices => _vertices.AsSpan();
        internal Span<uint> Indices => _indices.AsSpan();

        internal bool HasAnyDrawData => _sections.Count > 0;

        internal static Gizmos Instance => Unsafe.As<Gizmos>(s_instance.Target!);
    }

    internal readonly record struct GizmoVertex(Vector3 Position, Color Color);
    internal readonly record struct GizmoSection(int IndexStart, GizmosMode Mode);

    internal enum GizmosMode : byte
    {
        Solid = 0,
        Wire,

        Undefined
    }
}
