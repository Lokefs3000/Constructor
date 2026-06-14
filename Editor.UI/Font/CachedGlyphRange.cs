using CommunityToolkit.HighPerformance;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Editor.UI.Font
{
    public sealed class CachedGlyphRange
    {
        private readonly char _baseCharIndex;
        private Dictionary<byte, CachedGlyphBitmap> _bitmaps;

        private bool _isModified;
 
        internal CachedGlyphRange(char baseChar)
        {
            _baseCharIndex = baseChar;
            _bitmaps = new Dictionary<byte, CachedGlyphBitmap>();

            _isModified = false;
        }

        internal void ClearRange()
        {
            _bitmaps.Clear();
            _isModified = false;
        }

        internal void LoadFromStream(Stream stream)
        {
            GlyphRangeHeader header = stream.Read<GlyphRangeHeader>();

            if (header.FileHeader != GlyphRangeHeader.Header || header.FileVersion != GlyphRangeHeader.Version)
                throw new Exception("Invalid header or version");

            _bitmaps.EnsureCapacity(header.BitmapCount);

            for (int i = 0; i < header.BitmapCount; i++)
            {
                byte key = stream.Read<byte>();
                byte[] pixels = new byte[stream.Read<ushort>()];

                stream.ReadExactly(pixels);
                _bitmaps[key] = new CachedGlyphBitmap(pixels);
            }
        }

        internal void FlushToStream(Stream stream)
        {
            stream.Write(new GlyphRangeHeader
            {
                FileHeader = GlyphRangeHeader.Header,
                FileVersion = GlyphRangeHeader.Version,

                BitmapCount = (byte)_bitmaps.Count
            });

            foreach (var kvp in _bitmaps)
            {
                stream.WriteByte(kvp.Key);
                stream.Write((ushort)kvp.Value.Pixels.Length);
                stream.Write(kvp.Value.Pixels);
            }

            _isModified = false;
        }

        internal bool TryGetBitmap(char c, out CachedGlyphBitmap bitmap)
        {
            Debug.Assert(c >= _baseCharIndex && c <= _baseCharIndex + RangeSize);
            return _bitmaps.TryGetValue((byte)(c - _baseCharIndex), out bitmap);
        }

        internal void StoreBitmap(char c, CachedGlyphBitmap bitmap)
        {
            Debug.Assert(c >= _baseCharIndex && c <= _baseCharIndex + RangeSize);

            byte idx = (byte)(c - _baseCharIndex);
            _bitmaps[idx] = bitmap;
            _isModified = true;
        }

        internal void RemoveBitmap(char c)
        {
            Debug.Assert(c >= _baseCharIndex && c <= _baseCharIndex + RangeSize);

            byte idx = (byte)(c - _baseCharIndex);
            _bitmaps.Remove(idx);
            _isModified = true;
        }

        public char BaseCharIndex => _baseCharIndex;

        public bool IsEmpty => _bitmaps.Count == 0;
        public bool IsModified => _isModified;

        public const int RangeSize = 255;
    }

    public struct GlyphRangeHeader
    {
        public int FileHeader;
        public int FileVersion;

        public byte BitmapCount;

        public const int Header = 0x4e524c47;
        public const int Version = 1;
    }

    public readonly record struct CachedGlyphBitmap(byte[] Pixels);
}
