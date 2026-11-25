using Editor.UI.Assets;
using Primary.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Editor.UI.Text
{
    public readonly record struct ShapedTextIterator(ShapedTextData TextData) : IEnumerable<TextRenderSegment>
    {
        public Enumerator GetEnumerator() => new Enumerator(TextData);
        IEnumerator<TextRenderSegment> IEnumerable<TextRenderSegment>.GetEnumerator() => throw new NotSupportedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();

        public ref struct Enumerator : IEnumerator<TextRenderSegment>
        {
            private ShapedTextData _textData;

            private int _sectionIndex;
            private int _lineIndex;

            private TextRenderSegment _current;

            public Enumerator(ShapedTextData textData)
            {
                _textData = textData;

                _sectionIndex = textData.Sections.IsEmpty ? -1 : 0;
                _lineIndex = textData.Sections.IsEmpty ? 0 : textData.Sections[0].LineRange.Start;
            }

            public bool MoveNext()
            {
                if (_sectionIndex == -1 || _lineIndex == -1)
                    return false;

                ref readonly TextLineSection section = ref _textData.Sections[_sectionIndex];
                ref readonly TextLineData line = ref _textData.Lines[_lineIndex];

                IndexRange textRange = new IndexRange(
                    Math.Max(section.TextRange.Start, line.TextRange.Start),
                    Math.Min(section.TextRange.End, line.TextRange.End));

                Debug.Assert(section.LineRange.Start <= _lineIndex);
                Debug.Assert(!textRange.IsEmpty);
         
                _current = new TextRenderSegment(
                    line.LineOffset,
                    section.LeftOffset,
                    line.LineSize,
                    section.TypeData,
                    section.VisualInfo,
                    _textData.Letters.Slice(textRange.Start, textRange.Length));

                if (section.LineRange.End <= ++_lineIndex)
                {
                    if (++_sectionIndex == _textData.Sections.Length)
                        _sectionIndex = -1;
                    else
                        _lineIndex = _textData.Sections[_sectionIndex].LineRange.Start;
                }

                return true;
            }

            public void Dispose()
            {
                _sectionIndex = -1;
                _lineIndex = -1;

                _current = default;
            }

            public void Reset()
            {
                _sectionIndex = _textData.Sections.IsEmpty ? -1 : 0;
                _lineIndex = 0;

                _current = default;
            }

            public TextRenderSegment Current => _current;
            object IEnumerator.Current => throw new NotImplementedException();
        }
    }

    public ref struct TextRenderSegment
    {
        public readonly float LineOffset;
        public readonly float LeftOffset;
        public readonly Vector2 TextSize;
        public readonly UIFontTypeData TypeData;
        public readonly TextVisualInfo VisualInfo;
        public readonly ReadOnlySpan<char> Letters;

        public TextRenderSegment(float lineOffset, float leftOffset, Vector2 textSize, UIFontTypeData typeData, TextVisualInfo visualInfo, ReadOnlySpan<char> letters)
        {
            LineOffset = lineOffset;
            LeftOffset = leftOffset;
            TextSize = textSize;
            TypeData = typeData;
            VisualInfo = visualInfo;
            Letters = letters;
        }
    }
}
