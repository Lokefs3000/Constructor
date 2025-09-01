using Arch.Core;
using CommunityToolkit.HighPerformance;
using Primary.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Rendering
{
    public sealed class RenderScene
    {
        private List<RSOutputViewport> _outputViewports;

        internal RenderScene()
        {
            _outputViewports = new List<RSOutputViewport>();
        }

        internal void ClearInternalData()
        {
            _outputViewports.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddOutputViewport(RSOutputViewport viewport) => _outputViewports.Add(viewport);

        internal Span<RSOutputViewport> Viewports => _outputViewports.AsSpan();
    }

    public record struct RSOutputViewport
    {
        public long Id;

        public RHI.RenderTarget? Target;
        public Vector2 ClientSize;
        public Matrix4x4 ViewMatrix;
        public Matrix4x4 ProjectionMatrix;

        public Vector3 ViewPosition;
        public Vector3 ViewDirection;

        public Entity RootEntity;
    }
}
