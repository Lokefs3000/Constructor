using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;
using Primary.Collections.ReadOnly;
using Primary.Mathematics;

namespace EditorUI.Text.Visual
{
    public sealed class FontGlyphAtlas : IDisposable
    {
        private readonly IFontTextureFactory _factory;

        private Int2 _atlasSize;
        private List<PackingRect> _rects;

        private bool _hasAtlasResized;
        private IFontTexture? _fontTexture;

        private bool _disposedValue;

        internal FontGlyphAtlas(IFontTextureFactory factory)
        {
            _factory = factory;

            _atlasSize = Int2.Zero;
            _rects = new List<PackingRect>();

            _hasAtlasResized = false;
            _fontTexture = null;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _rects.Clear();

                    _fontTexture?.Dispose();
                    _fontTexture = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void FinishPacking()
        {
            if (_hasAtlasResized)
            {
                if (_fontTexture != null)
                    _fontTexture.Resize(_atlasSize);
                else
                    _fontTexture = _factory.CreateTexture(_atlasSize);
            }    
        }

        internal Int2 PackGlyphInto(Rect glyphRect)
        {
            Span<PackingRect> rects = _rects.AsSpan();
            for (int i = 0; i < rects.Length; i++)
            {
                ref PackingRect packingRect = ref rects[i];

                Int2 extents = packingRect.CurrentPosition + glyphRect.Size;

                bool fitsWithinX = extents.X <= packingRect.MaximumExtents.X;
                bool fitsWithinY = extents.Y <= packingRect.MaximumExtents.Y;

                if (fitsWithinX)
                {
                    Int2 position = Int2.Zero;
                    if (fitsWithinY)
                    {
                        position = packingRect.CurrentPosition;

                        packingRect.CurrentPosition = new Int2(extents.X, packingRect.CurrentPosition.Y);
                        packingRect.LineHeight = Math.Max(packingRect.LineHeight, glyphRect.Height);
                    }
                    else
                    {
                        if (packingRect.CurrentPosition.X + PackingIgnoreSizeW > packingRect.MaximumExtents.X)
                        {
                            packingRect.CurrentPosition = new Int2(packingRect.MinimumExtents.X + glyphRect.Width, packingRect.CurrentPosition.Y + packingRect.LineHeight);
                            packingRect.LineHeight = glyphRect.Height;

                            position = new Int2(packingRect.MinimumExtents.X, packingRect.CurrentPosition.Y);
                        }
                        else
                            continue;
                    }

                    if (packingRect.CurrentPosition.X + PackingMinimumSizeW > packingRect.MaximumExtents.X)
                    {
                        packingRect.CurrentPosition = new Int2(packingRect.MinimumExtents.X, packingRect.CurrentPosition.Y + packingRect.LineHeight);
                        packingRect.LineHeight = 0;

                        if (packingRect.CurrentPosition.Y + PackingMinimumSizeH > packingRect.MaximumExtents.Y)
                        {
                            _rects.RemoveAt(i);
                        }
                    }

                    return position;
                }
            }

            GrowCurrentAtlas();

            {
                rects = _rects.AsSpan();
                ref PackingRect packingRect = ref rects[^1];

                packingRect.CurrentPosition = new Int2(packingRect.CurrentPosition.X + glyphRect.Width, packingRect.CurrentPosition.Y);
                packingRect.LineHeight = glyphRect.Height;

                return packingRect.MinimumExtents;
            }
        }

        private void GrowCurrentAtlas()
        {
            if (_atlasSize.X == 0)
            {
                _atlasSize = new Int2(DefaultAtlasSize);
                _rects.Add(new PackingRect(Int2.Zero, Int2.Zero, _atlasSize, 0));
            }
            else
            {
                // if so, grow vertically
                if (_atlasSize.X > _atlasSize.Y)
                {
                    int prevHeight = _atlasSize.Y;
                    _atlasSize.Y *= 2;

                    _rects.Add(new PackingRect(new Int2(0, prevHeight), new Int2(0, prevHeight), _atlasSize, 0));
                }
                else
                {
                    int prevWidth = _atlasSize.X;
                    _atlasSize.X *= 2;

                    _rects.Add(new PackingRect(new Int2(prevWidth, 0), new Int2(prevWidth, 0), _atlasSize, 0));
                }
            }

            _hasAtlasResized = true;
        }

        public Int2 AtlasSize => _atlasSize;
        public ROList<PackingRect> Rects => _rects;

        public IFontTexture? FontTexture => _fontTexture;

        private const int PackingMinimumSizeW = 5;
        private const int PackingMinimumSizeH = 13;

        private const int PackingIgnoreSizeW = 33;

        private const int DefaultAtlasSize = 128;
    }

    public record struct PackingRect(Int2 CurrentPosition, Int2 MinimumExtents, Int2 MaximumExtents, int LineHeight);
}
