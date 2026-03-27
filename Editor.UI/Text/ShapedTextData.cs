using Collections.Pooled;
using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Visual;
using Primary.Collections;
using Primary.Common;
using Primary.Pooling;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace Editor.UI.Text
{
    public sealed class ShapedTextData
    {
        private char[] _letters;
        private int _letterIndex;

        private int _lastLineTextStart;

        private int _lastSectionLineStart;
        private int _lastSectionTextStart;

        private PooledList<TextLineData> _lines;
        private PooledList<TextLineSection> _sections;

        private Vector2 _totalSize;

        internal ShapedTextData()
        {
            _letters = Array.Empty<char>();
            _letterIndex = 0;

            _lastLineTextStart = 0;

            _lastSectionLineStart = 0;
            _lastSectionTextStart = 0;

            _lines = new PooledList<TextLineData>();
            _sections = new PooledList<TextLineSection>();

            _totalSize = Vector2.Zero;
        }

        internal void Clear()
        {
            _letterIndex = 0;

            _lastLineTextStart = 0;

            _lastSectionLineStart = 0;
            _lastSectionTextStart = 0;

            _lines.Clear();
            _sections.Clear();

            _totalSize = Vector2.Zero;
        }

        internal void InitializeLetterArray(int length)
        {
            if (_letters.Length < length)
                _letters = new char[length];
        }

        internal void SortSections()
        {
            _sections.Sort(TextLineSectionComparer.Default);
        }

        internal void SetMetrics(Vector2 totalSize)
        {
            _totalSize = totalSize;
        }

        internal void AddLetter(char letter)
        {
            _letters.DangerousGetReferenceAt(_letterIndex++) = letter;
        }

        internal void AddLetters(Span<char> letters)
        {
            if (letters.IsEmpty)
                return;

            if (TextWrapper.UseHardwareAcceleration)
            {
                if (Vector256.IsHardwareAccelerated && letters.Length > 16)
                {
                    int offset = 0;
                    int limit = letters.Length - 16;

                    do
                    {
                        Vector256.StoreUnsafe(
                            Vector256.LoadUnsafe(ref Unsafe.As<char, ushort>(ref letters.DangerousGetReferenceAt(offset))),
                            ref Unsafe.As<char, ushort>(ref _letters.DangerousGetReferenceAt(_letterIndex)));

                        offset += 16;
                        _letterIndex += 16;
                    } while (offset < limit);

                    if (offset == letters.Length)
                        return;

                    letters = letters.Slice(offset);
                }
                else if (Vector128.IsHardwareAccelerated && letters.Length > 8)
                {
                    int offset = 0;
                    int limit = letters.Length - 8;

                    do
                    {
                        Vector128.StoreUnsafe(
                            Vector128.LoadUnsafe(ref Unsafe.As<char, ushort>(ref letters.DangerousGetReferenceAt(offset))),
                            ref Unsafe.As<char, ushort>(ref _letters.DangerousGetReferenceAt(_letterIndex)));

                        offset += 8;
                        _letterIndex += 8;

                    } while (offset < limit);

                    if (offset == letters.Length)
                        return;

                    letters = letters.Slice(offset);
                }
            }

            {
                int counter = _letterIndex + letters.Length;
                int j = 0;

                do
                {
                    _letters.DangerousGetReferenceAt(_letterIndex++) = letters[j++];
                } while (_letterIndex != counter);
            }
        }

        internal void AddLine(int index, float offset, Vector2 size, bool artificial = false)
        {
            if (_lastLineTextStart == _letterIndex)
                return;

            _lines.Add(new TextLineData(index, offset, size, new IndexRange(_lastLineTextStart, _letterIndex)));
            _lastLineTextStart = _letterIndex;

            if (artificial)
                _lastSectionLineStart = _lines.Count;
        }

        internal void AddSection(UIFontStyle fontStyle, float leftOffset, TextVisualInfo visualInfo)
        {
            if (_lastSectionTextStart == _letterIndex && _lastSectionLineStart == _lines.Count)
                return;

            _sections.Add(new TextLineSection(fontStyle, leftOffset, visualInfo, new IndexRange(_lastSectionLineStart, _lines.Count), new IndexRange(_lastSectionTextStart, _letterIndex)));

            _lastSectionTextStart = _letterIndex;
            _lastSectionLineStart = _lines.Count;
        }

        public ShapedTextIterator Iterate() => new ShapedTextIterator(this);

        public Vector2 TotalSize => _totalSize;

        public ReadOnlySpan<char> Letters => _letters.AsSpan(0, _letterIndex);
        public ReadOnlySpan<TextLineData> Lines => _lines.Span;
        public ReadOnlySpan<TextLineSection> Sections => _sections.Span;

        public bool IsEmpty => _sections.Count == 0;

        internal readonly record struct Policy : IObjectPoolPolicy<ShapedTextData>
        {
            public ShapedTextData Create() => new ShapedTextData();
            public bool Return(ref ShapedTextData obj)
            {
                obj.Clear();
                return true;
            }
        }

        private class TextLineSectionComparer : IComparer<TextLineSection>
        {
            public int Compare(TextLineSection x, TextLineSection y)
            {
                return HashCode.Combine(x.FontStyle.Index, x.FontStyle.Font.Id).CompareTo(HashCode.Combine(y.FontStyle.Index, y.FontStyle.Font.Id));
            }

            public static readonly TextLineSectionComparer Default = new TextLineSectionComparer();
        }
    }

    public readonly record struct TextLineData(int LineIndex, float LineOffset, Vector2 LineSize, IndexRange TextRange);
    public readonly record struct TextLineSection(UIFontStyle FontStyle, float LeftOffset, TextVisualInfo VisualInfo, IndexRange LineRange, IndexRange TextRange);
    public readonly record struct TextVisualInfo(PaintColor DrawColor, float FontSize, UIFontStyle Style);

    public record struct MutableTextVisualInfo(PaintColor DrawColor, float FontSize, UIFontStyle Style)
    {
        public static implicit operator MutableTextVisualInfo(TextVisualInfo info) => Unsafe.ReadUnaligned<MutableTextVisualInfo>(ref Unsafe.As<TextVisualInfo, byte>(ref info));
        public static implicit operator TextVisualInfo(MutableTextVisualInfo info)
        {
            MutableTextVisualInfo temp = info;
            temp.FontSize *= TextManager.PixelsPerEM;

            return Unsafe.ReadUnaligned<TextVisualInfo>(ref Unsafe.As<MutableTextVisualInfo, byte>(ref temp));
        }
    }
}
