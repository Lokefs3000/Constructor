using Arch.LowLevel;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Rendering.Debuggable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.Rendering
{
    public sealed class Gizmos : IDisposable, IDebugRenderer
    {
        private static readonly WeakReference s_instance = new WeakReference(null);

        private List<GZDrawSection> _drawSections;
        private UnsafeList<GZVertex> _vertices;

        private int _currentVertexOffset;
        private GZVertexType _currentVertexType;

        private Stack<StackMatrix> _matrixStack;
        private bool _matriciesHaveChanged;

        private bool _disposedValue;

        internal Gizmos()
        {
            Debug.Assert(s_instance.Target == null);
            s_instance.Target = this;

            _drawSections = new List<GZDrawSection>();
            _vertices = new UnsafeList<GZVertex>(32);

            _currentVertexOffset = 0;
            _currentVertexType = unchecked((GZVertexType)(-1));

            _matrixStack = new Stack<StackMatrix>();
            _matriciesHaveChanged = false;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _vertices.Dispose();
                _vertices = new UnsafeList<GZVertex>();

                s_instance.Target = null;
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

        internal void ClearDrawData()
        {
            _drawSections.Clear(); 
            _vertices.Clear();

            _currentVertexOffset = 0;
            _currentVertexType = unchecked((GZVertexType)(-1));

            _matrixStack.Clear();
            _matriciesHaveChanged = true;
        }

        internal void FinishDrawData()
        {
            if (_vertices.Count > _currentVertexOffset)
            {
                AddNewSection(GZVertexType.Triangle);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ChangeActiveSectionIfNeeded(GZVertexType vt)
        {
            if (_vertices.Count > _currentVertexOffset && _currentVertexType != vt)
                AddNewSection(vt);
        }

        private void AddNewSection(GZVertexType vt)
        {
            if (_vertices.Count <= _currentVertexOffset)
                return;
            if (!_matrixStack.TryPeek(out StackMatrix matrix))
                matrix = new StackMatrix(Matrix4x4.Identity, true);

            _drawSections.Add(new GZDrawSection(_matriciesHaveChanged ? matrix.Matrix : null, matrix.Multiplied, _currentVertexType, _vertices.Count - _currentVertexOffset));
            
            _currentVertexOffset = _vertices.Count;
            _currentVertexType = vt;
            _matriciesHaveChanged = false;
        }

        #region Interface
        public void DrawLine(Vector3 from, Vector3 to, Color color)
        {
            ChangeActiveSectionIfNeeded(GZVertexType.Line);

            _vertices.Add(new GZVertex(from, color));
            _vertices.Add(new GZVertex(to, color));
        }

        public void DrawVector(Vector3 position)
        {
            ChangeActiveSectionIfNeeded(GZVertexType.Line);

            _vertices.Add(new GZVertex(position + Vector3.UnitX, Color.Red));
            _vertices.Add(new GZVertex(position + Vector3.UnitY, Color.Green));
            _vertices.Add(new GZVertex(position + Vector3.UnitZ, Color.Blue));
        }

        public void DrawWireSphere(Vector3 center, float radius, Color color)
        {
            ChangeActiveSectionIfNeeded(GZVertexType.Line);

            const int Steps = 16;
            const float StepMult = float.Pi * 2.0f / Steps;

            Vector3 lastPositionX = Vector3.Zero;
            Vector3 lastPositionY = Vector3.Zero;
            Vector3 lastPositionZ = Vector3.Zero;

            for (int i = 0; i < Steps; i++)
            {
                (float sin, float cos) = MathF.SinCos(i * StepMult);

                Vector2 sinCos = new Vector2(sin, cos) * radius;

                Vector3 positionX = center + new Vector3(0.0f, sinCos.X, sinCos.Y);
                Vector3 positionY = center + new Vector3(sinCos.X, 0.0f, sinCos.Y);
                Vector3 positionZ = center + new Vector3(sinCos.X, sinCos.Y, 0.0f);

                if (i > 0)
                {
                    _vertices.Add(new GZVertex(lastPositionX, color));
                    _vertices.Add(new GZVertex(positionX, color));

                    _vertices.Add(new GZVertex(lastPositionY, color));
                    _vertices.Add(new GZVertex(positionY, color));

                    _vertices.Add(new GZVertex(lastPositionZ, color));
                    _vertices.Add(new GZVertex(positionZ, color));
                }

                lastPositionX = positionX;
                lastPositionY = positionY;
                lastPositionZ = positionZ;
            }
        }

        public void DrawWireAABB(AABB aabb, Color color)
        {
            ChangeActiveSectionIfNeeded(GZVertexType.Line);

            _vertices.Add(new GZVertex(aabb.Minimum, color));
            _vertices.Add(new GZVertex(new Vector3(aabb.Maximum.X, aabb.Minimum.Y, aabb.Minimum.Z), color));

            _vertices.Add(new GZVertex(new Vector3(aabb.Minimum.X, aabb.Maximum.Y, aabb.Minimum.Z), color));
            _vertices.Add(new GZVertex(new Vector3(aabb.Maximum.X, aabb.Maximum.Y, aabb.Minimum.Z), color));

            _vertices.Add(new GZVertex(new Vector3(aabb.Minimum.X, aabb.Minimum.Y, aabb.Maximum.Z), color));
            _vertices.Add(new GZVertex(new Vector3(aabb.Maximum.X, aabb.Minimum.Y, aabb.Maximum.Z), color));

            _vertices.Add(new GZVertex(new Vector3(aabb.Minimum.X, aabb.Maximum.Y, aabb.Minimum.Z), color));
            _vertices.Add(new GZVertex(new Vector3(aabb.Maximum.X, aabb.Maximum.Y, aabb.Minimum.Z), color));

            _vertices.Add(new GZVertex(new Vector3(aabb.Minimum.X, aabb.Minimum.Y, aabb.Minimum.Z), color));
            _vertices.Add(new GZVertex(new Vector3(aabb.Minimum.X, aabb.Maximum.Y, aabb.Minimum.Z), color));

            _vertices.Add(new GZVertex(new Vector3(aabb.Maximum.X, aabb.Minimum.Y, aabb.Minimum.Z), color));
            _vertices.Add(new GZVertex(new Vector3(aabb.Maximum.X, aabb.Maximum.Y, aabb.Minimum.Z), color));

            _vertices.Add(new GZVertex(new Vector3(aabb.Minimum.X, aabb.Minimum.Y, aabb.Maximum.Z), color));
            _vertices.Add(new GZVertex(new Vector3(aabb.Minimum.X, aabb.Maximum.Y, aabb.Maximum.Z), color));

            _vertices.Add(new GZVertex(new Vector3(aabb.Maximum.X, aabb.Minimum.Y, aabb.Maximum.Z), color));
            _vertices.Add(new GZVertex(new Vector3(aabb.Maximum.X, aabb.Maximum.Y, aabb.Maximum.Z), color));
        }

        public void DrawWireCircle(Vector3 center, float radius, Color color)
        {
            ChangeActiveSectionIfNeeded(GZVertexType.Line);

            const int Steps = 32;
            const float StepMult = float.Pi * 2.0f / Steps;

            Vector3 lastPosition = Vector3.Zero;

            for (int i = 0; i < Steps; i++)
            {
                (float sin, float cos) = MathF.SinCos(i * StepMult);

                Vector2 sinCos = new Vector2(sin, cos) * radius;
                Vector3 position = center + new Vector3(0.0f, sinCos.X, sinCos.Y);

                if (i > 0)
                {
                    _vertices.Add(new GZVertex(lastPosition, color));
                    _vertices.Add(new GZVertex(position, color));
                }

                lastPosition = position;
            }
        }

        public void DrawWireRect(Vector2 min, Vector2 max, Color color)
        {
            ChangeActiveSectionIfNeeded(GZVertexType.Line);

            _vertices.Add(new GZVertex(new Vector3(min.X, min.Y, 0.0f), color));
            _vertices.Add(new GZVertex(new Vector3(max.X, min.Y, 0.0f), color));

            _vertices.Add(new GZVertex(new Vector3(min.X, max.Y, 0.0f), color));
            _vertices.Add(new GZVertex(new Vector3(max.X, max.Y, 0.0f), color));

            _vertices.Add(new GZVertex(new Vector3(min.X, min.Y, 0.0f), color));
            _vertices.Add(new GZVertex(new Vector3(min.X, max.Y, 0.0f), color));

            _vertices.Add(new GZVertex(new Vector3(max.X, min.Y, 0.0f), color));
            _vertices.Add(new GZVertex(new Vector3(max.X, max.Y, 0.0f), color));
        }

        public void DrawSolidRect(Vector2 min, Vector2 max, Color color)
        {
            ChangeActiveSectionIfNeeded(GZVertexType.Triangle);

            _vertices.Add(new GZVertex(new Vector3(min.X, min.Y, 0.0f), color));
            _vertices.Add(new GZVertex(new Vector3(min.X, max.Y, 0.0f), color));
            _vertices.Add(new GZVertex(new Vector3(max.X, min.Y, 0.0f), color));

            _vertices.Add(new GZVertex(new Vector3(max.X, min.Y, 0.0f), color));
            _vertices.Add(new GZVertex(new Vector3(min.X, max.Y, 0.0f), color));
            _vertices.Add(new GZVertex(new Vector3(max.X, max.Y, 0.0f), color));
        }

        public void PushMatrix(Matrix4x4 matrix, bool multiplyWithPrevious)
        {
            _matrixStack.Push(new StackMatrix(matrix, multiplyWithPrevious));
            _matriciesHaveChanged = true;
        }

        public void PopMatrix()
        {
            _matriciesHaveChanged = true;
            AddNewSection(GZVertexType.Triangle);

            _matrixStack.TryPop(out _);
        }
        #endregion

        internal Span<GZDrawSection> Sections => _drawSections.AsSpan();
        internal Span<GZVertex> Vertices => _vertices.AsSpan();

        internal bool HasDrawData => _drawSections.Count > 0 && _vertices.Count > 0;

        internal static Gizmos Instance => Unsafe.As<Gizmos>(s_instance.Target!);

        private readonly record struct StackMatrix(Matrix4x4 Matrix, bool Multiplied);
    }

    internal readonly record struct GZDrawSection(Matrix4x4? Matrix, bool NeedsProjection, GZVertexType VertexType, int VertexCount);
    internal readonly record struct GZVertex(Vector3 Position, Color Color);

    internal enum GZVertexType : byte
    {
        Triangle = 0,
        Line
    }
}
