using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Rendering
{
    public sealed class ScreenGizmos : IDisposable
    {
        private static WeakReference s_instance = new WeakReference(null);

        private List<ScreenGizmoSection> _sections;

        private List<ScreenGizmoVertex> _vertices;
        private List<ushort> _indices;

        private Color _primaryColor;

        private object? _currentResource;
        private ScreenGizmosMode _currentMode;

        internal ScreenGizmos()
        {
            s_instance.Target = this;

            _sections = new List<ScreenGizmoSection>();

            _vertices = new List<ScreenGizmoVertex>();
            _indices = new List<ushort>();

            _primaryColor = Color.White;

            _currentResource = null;
            _currentMode = ScreenGizmosMode.Undefined;
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

            _currentResource = null;
            _currentMode = ScreenGizmosMode.Undefined;
        }

        private void CheckCurrentSection(object? resource, ScreenGizmosMode mode)
        {
            if (_currentResource != resource || _currentMode != mode)
            {
                _sections.Add(new ScreenGizmoSection(_indices.Count, mode, resource));

                _currentResource = resource;
                _currentMode = mode;
            }
        }

        #region Basic
        public static void DrawLine(Vector2 from, Vector2 to, Color? color = null)
        {
            ScreenGizmos self = Instance;
            self.CheckCurrentSection(null, ScreenGizmosMode.Wire);

            Color c = color.GetValueOrDefault(self._primaryColor);

            self._indices.Add((ushort)self._vertices.Count);
            self._indices.Add((ushort)(self._vertices.Count + 1));

            self._vertices.Add(new ScreenGizmoVertex(from, c));
            self._vertices.Add(new ScreenGizmoVertex(to, c));
        }

        public static void DrawVector(Vector2 position)
        {
            ScreenGizmos self = Instance;
            self.CheckCurrentSection(null, ScreenGizmosMode.Wire);

            self._indices.Add((ushort)self._vertices.Count);
            self._indices.Add((ushort)(self._vertices.Count + 2));
            self._indices.Add((ushort)(self._vertices.Count + 1));
            self._indices.Add((ushort)(self._vertices.Count + 3));

            self._vertices.Add(new ScreenGizmoVertex(position, Color.Red));
            self._vertices.Add(new ScreenGizmoVertex(position, Color.Green));
            self._vertices.Add(new ScreenGizmoVertex(position + new Vector2(32.0f, 0.0f), Color.Red));
            self._vertices.Add(new ScreenGizmoVertex(position + new Vector2(0.0f, 32.0f), Color.Green));
        }
        #endregion
        #region Wire
        public static void DrawWireRect(Vector2 min, Vector2 max, Color? color = null)
        {
            ScreenGizmos self = Instance;
            self.CheckCurrentSection(null, ScreenGizmosMode.Wire);

            Color c = color.GetValueOrDefault(self._primaryColor);

            ushort i0 = (ushort)self._vertices.Count;
            ushort i1 = (ushort)(self._vertices.Count + 1);
            ushort i2 = (ushort)(self._vertices.Count + 2);
            ushort i3 = (ushort)(self._vertices.Count + 3);

            self._indices.Add(i0);
            self._indices.Add(i1);

            self._indices.Add(i1);
            self._indices.Add(i2);

            self._indices.Add(i2);
            self._indices.Add(i3);

            self._indices.Add(i3);
            self._indices.Add(i0);

            self._vertices.Add(new ScreenGizmoVertex(min, c));
            self._vertices.Add(new ScreenGizmoVertex(new Vector2(max.X, min.Y), c));
            self._vertices.Add(new ScreenGizmoVertex(max, c));
            self._vertices.Add(new ScreenGizmoVertex(new Vector2(min.X, max.Y), c));
        }

        public static void DrawWireCircle(Vector2 center, float radius, Color? color = null)
        {
            ScreenGizmos self = Instance;
            self.CheckCurrentSection(null, ScreenGizmosMode.Wire);

            Color c = color.GetValueOrDefault(self._primaryColor);

            const float IndexToRad = MathF.PI * 2.0f / 11.0f;
            for (int i = 0; i < 12; i++)
            {
                float rad = i * IndexToRad;

                (float sin, float cos) = MathF.SinCos(rad);
                if (i > 0)
                {
                    self._indices.Add((ushort)self._vertices.Count);
                    self._indices.Add((ushort)(self._vertices.Count + 1));
                }

                self._vertices.Add(new ScreenGizmoVertex(new Vector2(sin, cos) * radius + center, c));
            }
        }

        public static void DrawWireTriangle(Vector2 posA, Vector2 posB, Vector2 posC, Color? color = null)
        {
            ScreenGizmos self = Instance;
            self.CheckCurrentSection(null, ScreenGizmosMode.Wire);

            Color c = color.GetValueOrDefault(self._primaryColor);

            ushort i0 = (ushort)self._vertices.Count;
            ushort i1 = (ushort)(self._vertices.Count + 1);
            ushort i2 = (ushort)(self._vertices.Count + 2);

            self._indices.Add(i0);
            self._indices.Add(i1);

            self._indices.Add(i1);
            self._indices.Add(i2);

            self._indices.Add(i2);
            self._indices.Add(i0);

            self._vertices.Add(new ScreenGizmoVertex(posA, c));
            self._vertices.Add(new ScreenGizmoVertex(posB, c));
            self._vertices.Add(new ScreenGizmoVertex(posC, c));
        }

        public static void DrawWireRect(Boundaries bounds, Color? color = null) => DrawWireRect(bounds.Minimum, bounds.Maximum, color);
        #endregion
        #region Solid
        public static void DrawSolidRect(Vector2 min, Vector2 max, Color? color = null)
        {
            ScreenGizmos self = Instance;
            self.CheckCurrentSection(null, ScreenGizmosMode.Solid);

            Color c = color.GetValueOrDefault(self._primaryColor);

            ushort i0 = (ushort)self._vertices.Count;
            ushort i1 = (ushort)(self._vertices.Count + 1);
            ushort i2 = (ushort)(self._vertices.Count + 2);
            ushort i3 = (ushort)(self._vertices.Count + 3);

            self._indices.Add(i0);
            self._indices.Add(i2);
            self._indices.Add(i1);

            self._indices.Add(i1);
            self._indices.Add(i3);
            self._indices.Add(i2);

            self._vertices.Add(new ScreenGizmoVertex(min, c));
            self._vertices.Add(new ScreenGizmoVertex(new Vector2(max.X, min.Y), c));
            self._vertices.Add(new ScreenGizmoVertex(new Vector2(min.X, max.Y), c));
            self._vertices.Add(new ScreenGizmoVertex(max, c));
        }

        public static void DrawSolidCircle(Vector2 center, float radius, Color? color = null)
        {
            ScreenGizmos self = Instance;
            self.CheckCurrentSection(null, ScreenGizmosMode.Solid);

            Color c = color.GetValueOrDefault(self._primaryColor);

            ushort i0 = (ushort)self._vertices.Count;

            self._vertices.Add(new ScreenGizmoVertex(center, c));

            const float IndexToRad = MathF.PI * 2.0f / 11.0f;
            for (int i = 0; i < 12; i++)
            {
                float rad = i * IndexToRad;

                (float sin, float cos) = MathF.SinCos(rad);
                if (i > 0)
                {
                    self._indices.Add((ushort)self._vertices.Count);
                    self._indices.Add(i0);
                    self._indices.Add((ushort)(self._vertices.Count + 1));
                }

                self._vertices.Add(new ScreenGizmoVertex(new Vector2(sin, cos) * radius + center, c));
            }
        }

        public static void DrawSolidTriangle(Vector2 posA, Vector2 posB, Vector2 posC, Color? color = null)
        {
            ScreenGizmos self = Instance;
            self.CheckCurrentSection(null, ScreenGizmosMode.Solid);

            Color c = color.GetValueOrDefault(self._primaryColor);

            self._indices.Add((ushort)self._vertices.Count);
            self._indices.Add((ushort)(self._vertices.Count + 1));
            self._indices.Add((ushort)(self._vertices.Count + 2));

            self._vertices.Add(new ScreenGizmoVertex(posA, c));
            self._vertices.Add(new ScreenGizmoVertex(posB, c));
            self._vertices.Add(new ScreenGizmoVertex(posC, c));
        }

        public static void DrawSolidRect(Boundaries bounds, Color? color = null) => DrawSolidRect(bounds.Minimum, bounds.Maximum, color);
        #endregion
        #region Properties
        public static Color Color { get => Instance._primaryColor; set => Instance._primaryColor = value; }
        #endregion

        internal Span<ScreenGizmoSection> Sections => _sections.AsSpan();

        internal Span<ScreenGizmoVertex> Vertices => _vertices.AsSpan();
        internal Span<ushort> Indices => _indices.AsSpan();

        internal bool HasAnyDrawData => _sections.Count > 0;

        internal static ScreenGizmos Instance => Unsafe.As<ScreenGizmos>(s_instance.Target)!;
    }

    internal readonly record struct ScreenGizmoVertex(Vector2 Position, Color Color);
    internal readonly record struct ScreenGizmoSection(int IndexStart, ScreenGizmosMode Mode, object? Resource);

    internal enum ScreenGizmosMode : byte
    {
        Solid = 0,
        Wire,

        Undefined
    }
}
