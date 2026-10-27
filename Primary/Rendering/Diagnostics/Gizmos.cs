using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Mathematics;
using TerraFX.Interop.Windows;

namespace Primary.Rendering.Diagnostics
{
    public sealed class Gizmos : IDisposable
    {
        private static readonly WeakReference s_instance = new WeakReference(null);

        private readonly GizmoFont _font;

        private readonly List<GizmoSection> _sections;

        private readonly List<GizmoVertex> _vertices;
        private readonly List<uint> _indices;

        private Color _primaryColor;

        private GizmosMode _currentMode;

        internal Gizmos(GizmoFont font)
        {
            s_instance.Target = this;

            _font = font;

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

            self._vertices.Add(new GizmoVertex(from, Vector2.Zero, c));
            self._vertices.Add(new GizmoVertex(to, Vector2.Zero, c));
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

            self._vertices.Add(new GizmoVertex(position, Vector2.Zero, Color.Red));
            self._vertices.Add(new GizmoVertex(position, Vector2.Zero, Color.Green));
            self._vertices.Add(new GizmoVertex(position, Vector2.Zero, Color.Blue));

            self._vertices.Add(new GizmoVertex(position + Vector3.UnitX, Vector2.Zero, Color.Red));
            self._vertices.Add(new GizmoVertex(position + Vector3.UnitY, Vector2.Zero, Color.Green));
            self._vertices.Add(new GizmoVertex(position + Vector3.UnitZ, Vector2.Zero, Color.Blue));
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

            self._vertices.Add(new GizmoVertex(pointA, Vector2.Zero, c));
            self._vertices.Add(new GizmoVertex(pointB, Vector2.Zero, c));
            self._vertices.Add(new GizmoVertex(pointC, Vector2.Zero, c));
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

                self._vertices.Add(new GizmoVertex(positionX, Vector2.Zero, c));
                self._vertices.Add(new GizmoVertex(positionY, Vector2.Zero, c));
                self._vertices.Add(new GizmoVertex(positionZ, Vector2.Zero, c));
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

            self._vertices.Add(new GizmoVertex(aabb.Minimum, Vector2.Zero, c));                                                   // ---      0
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Maximum.X, aabb.Minimum.Y, aabb.Minimum.Z), Vector2.Zero, c));    // +--      1
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Maximum.X, aabb.Maximum.Y, aabb.Minimum.Z), Vector2.Zero, c));    // ++-      2
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Minimum.X, aabb.Maximum.Y, aabb.Minimum.Z), Vector2.Zero, c));    // -+-      3
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Maximum.X, aabb.Minimum.Y, aabb.Maximum.Z), Vector2.Zero, c));    // +-+      4
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Minimum.X, aabb.Minimum.Y, aabb.Maximum.Z), Vector2.Zero, c));    // --+      5
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Minimum.X, aabb.Maximum.Y, aabb.Maximum.Z), Vector2.Zero, c));    // -++      6
            self._vertices.Add(new GizmoVertex(aabb.Maximum, Vector2.Zero, c));                                                   // +++      7
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

            self._vertices.Add(new GizmoVertex(aabb.Minimum, Vector2.Zero, c));                                                   // ---      0
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Maximum.X, aabb.Minimum.Y, aabb.Minimum.Z), Vector2.Zero, c));    // +--      1
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Maximum.X, aabb.Maximum.Y, aabb.Minimum.Z), Vector2.Zero, c));    // ++-      2
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Minimum.X, aabb.Maximum.Y, aabb.Minimum.Z), Vector2.Zero, c));    // -+-      3
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Maximum.X, aabb.Minimum.Y, aabb.Maximum.Z), Vector2.Zero, c));    // +-+      4
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Minimum.X, aabb.Minimum.Y, aabb.Maximum.Z), Vector2.Zero, c));    // --+      5
            self._vertices.Add(new GizmoVertex(new Vector3(aabb.Minimum.X, aabb.Maximum.Y, aabb.Maximum.Z), Vector2.Zero, c));    // -++      6
            self._vertices.Add(new GizmoVertex(aabb.Maximum, Vector2.Zero, c));                                                   // +++      7
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

            self._vertices.Add(new GizmoVertex(pointA, Vector2.Zero, c));
            self._vertices.Add(new GizmoVertex(pointB, Vector2.Zero, c));
            self._vertices.Add(new GizmoVertex(pointC, Vector2.Zero, c));
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

                self._vertices.Add(new GizmoVertex(position, Vector2.Zero, c));
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

            self._vertices.Add(new GizmoVertex(new Vector3(min.X, min.Y, 0.0f), Vector2.Zero, c));
            self._vertices.Add(new GizmoVertex(new Vector3(max.X, min.Y, 0.0f), Vector2.Zero, c));
            self._vertices.Add(new GizmoVertex(new Vector3(max.X, max.Y, 0.0f), Vector2.Zero, c));
            self._vertices.Add(new GizmoVertex(new Vector3(min.X, max.Y, 0.0f), Vector2.Zero, c));
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

            self._vertices.Add(new GizmoVertex(new Vector3(min.X, min.Y, 0.0f), Vector2.Zero, c));
            self._vertices.Add(new GizmoVertex(new Vector3(max.X, min.Y, 0.0f), Vector2.Zero, c));
            self._vertices.Add(new GizmoVertex(new Vector3(max.X, max.Y, 0.0f), Vector2.Zero, c));
            self._vertices.Add(new GizmoVertex(new Vector3(min.X, max.Y, 0.0f), Vector2.Zero, c));
        }

        public static void DrawText(Vector3 topLeft, ReadOnlySpan<byte> text, Color? color = null)
        {
            if (text.IsEmpty)
                return;

            Gizmos self = Instance;
            self.CheckCurrentSection(GizmosMode.Text);

            Color c = color.GetValueOrDefault(self._primaryColor);
            Vector2 glyphSize = new Vector2(GizmoFont.GlyphWidth * 0.1f, GizmoFont.GlyphHeight * 0.1f);

            Vector2 cursor = topLeft.AsVector2();
            for (int i = 0; i < text.Length; ++i)
            {
                char letter = (char)text.DangerousGetReferenceAt(i);
                if (letter == '\n')
                {
                    cursor.X = topLeft.X;
                    cursor.Y += GizmoFont.GlyphLineHeight * 0.1f;
                    continue;
                }
                else if (letter != ' ' && self._font.TryFindGlyph(letter, out GizmoGlyph glyph))
                {
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

                    Vector2 max = cursor + glyphSize;

                    self._vertices.Add(new GizmoVertex(new Vector3(cursor.X, cursor.Y, topLeft.Z), glyph.UVMinimum, c));
                    self._vertices.Add(new GizmoVertex(new Vector3(max.X, cursor.Y, topLeft.Z), glyph.UVMinimum.WithElement(0, glyph.UVMaximum.X), c));
                    self._vertices.Add(new GizmoVertex(new Vector3(max.X, max.Y, topLeft.Z), glyph.UVMaximum, c));
                    self._vertices.Add(new GizmoVertex(new Vector3(cursor.X, max.Y, topLeft.Z), glyph.UVMinimum.WithElement(1, glyph.UVMaximum.Y), c));
                }

                cursor.X += GizmoFont.GlyphAdvance * 0.1f;
            }
        }

        public static void DrawText(Vector3 topLeft, ReadOnlySpan<char> text, Color? color = null)
        {
            if (text.IsEmpty)
                return;

            Gizmos self = Instance;
            self.CheckCurrentSection(GizmosMode.Text);

            Color c = color.GetValueOrDefault(self._primaryColor);
            Vector2 glyphSize = new Vector2(GizmoFont.GlyphWidth * 0.1f, GizmoFont.GlyphHeight * 0.1f);

            Vector2 cursor = topLeft.AsVector2();
            for (int i = 0; i < text.Length; ++i)
            {
                char letter = text.DangerousGetReferenceAt(i);
                if (letter == '\n')
                {
                    cursor.X = topLeft.X;
                    cursor.Y += GizmoFont.GlyphLineHeight * 0.1f;
                    continue;
                }
                else if (letter != ' ' && self._font.TryFindGlyph(letter, out GizmoGlyph glyph))
                {
                    uint i0 = (uint)self._vertices.Count;
                    uint i1 = (uint)(self._vertices.Count + 1);
                    uint i2 = (uint)(self._vertices.Count + 2);
                    uint i3 = (uint)(self._vertices.Count + 3);

                    self._indices.Add(i0);
                    self._indices.Add(i2);
                    self._indices.Add(i1);

                    self._indices.Add(i0);
                    self._indices.Add(i3);
                    self._indices.Add(i2);

                    Vector2 max = cursor + glyphSize;

                    self._vertices.Add(new GizmoVertex(new Vector3(cursor.X, cursor.Y, topLeft.Z), glyph.UVMinimum, c));
                    self._vertices.Add(new GizmoVertex(new Vector3(max.X, cursor.Y, topLeft.Z), glyph.UVMinimum.WithElement(0, glyph.UVMaximum.X), c));
                    self._vertices.Add(new GizmoVertex(new Vector3(max.X, max.Y, topLeft.Z), glyph.UVMaximum, c));
                    self._vertices.Add(new GizmoVertex(new Vector3(cursor.X, max.Y, topLeft.Z), glyph.UVMinimum.WithElement(1, glyph.UVMaximum.Y), c));
                }

                cursor.X += GizmoFont.GlyphAdvance * 0.1f;
            }
        }
        #endregion

        public static GizmoFont Font => Instance._font;

        internal Span<GizmoSection> Sections => _sections.AsSpan();

        internal Span<GizmoVertex> Vertices => _vertices.AsSpan();
        internal Span<uint> Indices => _indices.AsSpan();

        internal bool HasAnyDrawData => _sections.Count > 0;

        internal static Gizmos Instance => Unsafe.As<Gizmos>(s_instance.Target!);
    }

    internal readonly record struct GizmoVertex(Vector3 Position, Vector2 UV, Color Color);
    internal readonly record struct GizmoSection(int IndexStart, GizmosMode Mode);

    internal enum GizmosMode : byte
    {
        Solid = 0,
        Wire,
        Text,

        Undefined
    }
}
