using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Helpers;
using Editor.UI.Text;
using Primary.Assets;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Visual
{
    public sealed class UICommandBuffer : IDisposable
    {
        private readonly UIRenderer _renderer;

        private readonly ObjectIndexer _indexer;
        private readonly StringAllocator _strings;

        private List<UIDrawCommand> _commands;
        private Boundaries _drawBounds;

        private bool _disposedValue;

        internal UICommandBuffer(UIRenderer renderer)
        {
            _renderer = renderer;

            _indexer = new ObjectIndexer();
            _strings = new StringAllocator(1024);

            _commands = new List<UIDrawCommand>();
            _drawBounds = Boundaries.Zero;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _strings.Dispose();
                    _indexer.Clear();
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

            _indexer.Clear();
            _strings.Reset();
        }

        public void AddRectangle(int zIndex, Boundaries bounds, UIColor color, float rounding = 0.0f, UIRoundedCorner corners = UIRoundedCorner.All)
        {
            if (!bounds.IsIntersecting(_drawBounds))
                return;

            _commands.Add(new UIDrawCommand(_commands.Count, zIndex, new UIDrawRectangle
            {
                DrawBounds = bounds,
                Color = color.Type == UIColorType.Solid ? new UIDrawColor(color.Solid) : new UIDrawColor(_renderer.GradientManager.RegisterGradient(color.Gradient)),

                CornersToRound = corners,
                Rounding = rounding,
            }));
        }

        ///<summary>
        ///<code>
        ///   B
        ///  / \
        /// /   \
        /// A---C
        ///</code>
        ///</summary>
        public void AddTriangle(int zIndex, Vector2 a, Vector2 b, Vector2 c, UIColor color, float rounding = 0.0f)
        {
            _commands.Add(new UIDrawCommand(_commands.Count, zIndex, new UIDrawTriangle
            {
                A = a,
                B = b,
                C = c,
                Color = color.Type == UIColorType.Solid ? new UIDrawColor(color.Solid) : new UIDrawColor(_renderer.GradientManager.RegisterGradient(color.Gradient)),

                InfillWidth = 0.0f,

                Rounding = rounding,
            }));
        }

        public void AddCircle(int zIndex, Vector2 center, float radius, UIColor color)
        {
            _commands.Add(new UIDrawCommand(_commands.Count, zIndex, new UIDrawCircle
            {
                Center = center,
                Radius = radius,
                Color = color.Type == UIColorType.Solid ? new UIDrawColor(color.Solid) : new UIDrawColor(_renderer.GradientManager.RegisterGradient(color.Gradient)),

                InfillRadius = 0.0f,
            }));
        }

        public void AddStroke(int zIndex, Boundaries bounds, UIColor color, float weight, float roundingPerc = 0.0f, UIRoundedCorner corners = UIRoundedCorner.All)
        {
            if (!bounds.IsIntersecting(_drawBounds))
                return;
        }

        public void AddText(int zIndex, Boundaries bounds, UIColor color, UITextShapingData shapingData, float size, float lineHeight, float letterSpacing, UITextAlignment alignment = UITextAlignment.Top | UITextAlignment.Left)
        {
            
        }

        public void AddSimpleText(int zIndex, Vector2 minimum, UIColor color, UIFontStyle fontStyle, string text, float size)
        {
            //HACK: warmup glyphs as they are lazy loaded. this should change to be instant because of this. nice thought but a pain later

            for (int i = 0; i < text.Length; ++i)
                fontStyle.RequestGlyph(text.DangerousGetReferenceAt(i));

            _commands.Add(new UIDrawCommand(_commands.Count, zIndex, new UIDrawSimpleText
            {
                Position = minimum,
                Color = color.Type == UIColorType.Solid ? new UIDrawColor(color.Solid) : new UIDrawColor(_renderer.GradientManager.RegisterGradient(color.Gradient)),

                FontStyleIndex = _indexer.Add(fontStyle),

                Text = _strings.Allocate(text),
                TextScale = size
            }));
        }

        public void AddImage(int zIndex, Boundaries bounds, UIColor tint, TextureAsset image)
        {
            if (!bounds.IsIntersecting(_drawBounds))
                return;
        }

        internal void CopyCommandsTo(Span<UIDrawCommand> commands) => _commands.CopyTo(commands);

        internal void CopyInternalsTo(UICommandBuffer commandBuffer)
        {
            commandBuffer.ClearCommands(_drawBounds);

            _indexer.CopyTo(commandBuffer._indexer);
            _strings.CopyTo(commandBuffer._strings);
            
            commandBuffer._commands.AddRange(_commands.AsSpan());
        }

        internal ObjectIndexer Indexer => _indexer;
        internal StringAllocator Strings => _strings;

        internal IReadOnlyList<UIDrawCommand> Commands => _commands;

        public Boundaries DrawBoundaries => _drawBounds;
    }
}
