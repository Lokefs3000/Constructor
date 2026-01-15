using Editor.UI.Assets;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Text
{
    public sealed class UITextShapingData
    {
        private UIFontStyle? _style;
        private string? _text;

        private Vector2 _totalSize;

        private UITextShapingLine[] _lines;
        private int _lineCount;

        public UITextShapingData()
        {
            _style = null;

            _totalSize = Vector2.Zero;

            _lines = Array.Empty<UITextShapingLine>();
            _lineCount = 0;
        }

        internal void ClearPreviousData(UIFontStyle? style = null, string? text = null)
        {
            _style = style;
            _text = text;

            _totalSize = Vector2.Zero;

            Array.Clear(_lines);
            _lineCount = 0;
        }

        internal void AddLine(Vector2 size, IndexRange range)
        {
            if (_lines.Length <= _lineCount)
                Array.Resize(ref _lines, (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(_lineCount + 1, 1)));

            _lines[_lineCount] = new UITextShapingLine(_lineCount, size, range);
            ++_lineCount;
        }

        internal void SetTotalSize(Vector2 size) => _totalSize = size;

        public Vector2 TotalSize => _totalSize;
        public ReadOnlySpan<UITextShapingLine> Lines => _lines.AsSpan(0, _lineCount);
    }

    public readonly record struct UITextShapingLine(int LineIndex, Vector2 LineSize, IndexRange TextRange);
}
