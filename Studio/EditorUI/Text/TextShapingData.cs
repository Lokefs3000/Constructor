using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Built;
using EditorUI.Text.Visual;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;

namespace EditorUI.Text
{
    public sealed class TextShapingData
    {
        private char[] _textBuffer;
        private int _textLength;

        private int _lastLineTextStart;
        private int _lastSectionTextStart;
        private int _lastSectionVisualGlyphs;

        private List<TextShapingLine> _lines;
        private List<TextShapingSection> _sections;

        private Vector2 _totalSize;

        private ShapingDataKey _dataKey;
        private int _glyphsWithActualVisual;

        internal TextShapingData()
        {
            _textBuffer = [];
            _textLength = 0;

            _lastSectionTextStart = 0;
            _lastLineTextStart = 0;
            _lastSectionVisualGlyphs = 0;

            _lines = new List<TextShapingLine>();
            _sections = new List<TextShapingSection>();

            _totalSize = Vector2.Zero;

            _dataKey = default;
            _glyphsWithActualVisual = 0;
        }

        internal void ClearInternalValues()
        {
            _textLength = 0;

            _lastSectionTextStart = 0;
            _lastLineTextStart = 0;
            _lastSectionVisualGlyphs = 0;

            _lines.Clear();
            _sections.Clear();

            _totalSize = Vector2.Zero;

            _dataKey = default;
            _glyphsWithActualVisual = 0;
        }

        internal void SetDataKey(ShapingDataKey dataKey)
        {
            _dataKey = dataKey;
        }

        internal void SetRenderData(int glyphsWithActualVisual)
        {
            _glyphsWithActualVisual = glyphsWithActualVisual;
        }

        internal void AllocateLettersFor(int letterCount)
        {
            if (_textBuffer.Length < letterCount)
            {
                Array.Resize(ref _textBuffer, (int)(letterCount * 1.2));
            }
        }

        internal void CalculateMetrics()
        {
            _totalSize = Vector2.Zero;
            foreach (TextShapingLine line in _lines)
            {
                _totalSize = Vector2.Max(_totalSize, line.LineSize + new Vector2(0.0f, line.LineYOffset));
            }
        }

        internal void AddLetters(ReadOnlySpan<char> letters)
        {
            // the old version of this method had a cascading line of statments like:
            //  if (Vector256.IsHardwareAccelerated && letters.Length > 16)
            //      ..
            //  else if (Vector128.IsHardwareAccelerated && letters.Length > 8)
            //      ..
            // and then a loop to finish it off and i don't think it's worth it to do again

            if (letters.IsEmpty)
                return;

            letters.CopyTo(_textBuffer.AsSpan(_textLength));
            _textLength += letters.Length;
        }

        internal void AddLetter(char letter)
        {
            _textBuffer[_textLength++] = letter;
        }

        internal void AddLine(Vector2 lineSize, float lineYOffset)
        {
            if (_lastLineTextStart == _textLength)
                return;

            _lines.Add(new TextShapingLine(lineSize, lineYOffset));
            _lastLineTextStart = _textLength;
        }

        internal void AddSection(float leftOffset, TextShapingVisual visual, int currentVisualGlyphs)
        {
            if (_lastSectionTextStart == _textLength)
                return;

            _sections.Add(new TextShapingSection(_lines.Count, new IndexRange(_lastSectionTextStart, _textLength), leftOffset, visual, currentVisualGlyphs - _lastSectionVisualGlyphs));
            
            _lastSectionTextStart = _textLength;
            _lastSectionVisualGlyphs = currentVisualGlyphs;
        }

        public ReadOnlySpan<char> TextBuffer => _textBuffer.AsSpan(0, _textLength);
        public ROList<TextShapingLine> Lines => _lines;
        public ROList<TextShapingSection> Sections => _sections;

        public Vector2 TotalSize => _totalSize;

        public BuiltTextBuilder TextBuilder => _dataKey.TextBuilder;
        public FontStyleData StyleData => _dataKey.StyleData;
        public float PixelSize => _dataKey.PixelSize;

        public int GlyphsWithActualVisual => _glyphsWithActualVisual;
    }

    public readonly record struct TextShapingLine(Vector2 LineSize, float LineYOffset);
    public readonly record struct TextShapingSection(int LinePosition, IndexRange TextRange, float LeftOffset, TextShapingVisual Visual, int GlyphsWithActualVisual);
    public readonly record struct TextShapingVisual(BuiltPaint? Paint, float PixelSize, FontStyleData StyleData);
}
