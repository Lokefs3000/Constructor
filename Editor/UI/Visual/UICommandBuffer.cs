using Editor.UI.Datatypes;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Visual
{
    public sealed class UICommandBuffer : IDisposable
    {
        private readonly UIRenderer _renderer;

        private List<UIDrawCommand> _commands;
        private Boundaries _drawBounds;

        private bool _disposedValue;

        internal UICommandBuffer(UIRenderer renderer)
        {
            _renderer = renderer;

            _commands = new List<UIDrawCommand>();
            _drawBounds = Boundaries.Zero;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void ClearCommands(Boundaries newDrawBounds)
        {
            _commands.Clear();
            _drawBounds = newDrawBounds;
        }

        public void AddRectangle(int zIndex, Boundaries bounds, UIColor color, float roundingPerc = 0.0f, UIRoundedCorner corners = UIRoundedCorner.All)
        {
            if (!bounds.IsIntersecting(_drawBounds))
                return;

            _commands.Add(new UIDrawCommand(_commands.Count, zIndex, new UIDrawRectangle
            {
                DrawBounds = bounds,
                Color = color.Type == UIColorType.Solid ? new UIDrawColor(color.Solid) : new UIDrawColor(_renderer.GradientManager.RegisterGradient(color.Gradient)),

                CornersToRound = corners,
                RoundingPerc = roundingPerc
            }));
        }

        public void AddBorder(int zIndex, Boundaries bounds, UIColor color, UIStrokePosition position, float weight, float roundingPerc = 0.0f, UIRoundedCorner corners = UIRoundedCorner.All)
        {

        }

        internal void CopyCommandsTo(Span<UIDrawCommand> commands) => _commands.CopyTo(commands);

        internal IReadOnlyList<UIDrawCommand> Commands => _commands;

        public Boundaries DrawBoundaries => _drawBounds;
    }
}
