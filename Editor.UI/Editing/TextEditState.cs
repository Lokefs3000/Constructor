using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Text;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using System.Diagnostics;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

namespace Editor.UI.Editing
{
    public sealed class TextEditState
    {
        private readonly StringPieceTable _pieceTable;

        private TextEditActiveState _editState;

        private UIFontTypeData? _fontTypeData;
        private float _fontSize;

        private List<TextSelectionCursor> _cursors;

        private string? _cachedString;

        public TextEditState(StringPieceTable pieceTable, UIFontTypeData? typeData = null)
        {
            _pieceTable = pieceTable;

            _editState = TextEditActiveState.Inactive;

            _fontTypeData = typeData;
            _fontSize = 1.0f;

            _cursors = new List<TextSelectionCursor>();

            _cachedString = null;
        }

        internal void Activate()
        {
            _editState = TextEditActiveState.Default;
        }

        internal void Finish(bool isEditFinished)
        {
            _editState = TextEditActiveState.Inactive;
            _cursors.Clear();

            FocusLost?.Invoke(isEditFinished);
        }

        public void ClearNewText(string text)
        {
            _cachedString = text;
            _cursors.Clear();
        }

        public void WriteText(ReadOnlySpan<char> text)
        {
            if (_cursors.Exists(static (x) => !x.IsSingle))
                DeleteSelected(true);
            if (text.IsEmpty)
                return;

            int selectionOffset = 0;

            Span<TextSelectionCursor> cursors = _cursors.AsSpan();
            for (int i = 0; i < cursors.Length; i++)
            {
                ref TextSelectionCursor cursor = ref cursors[i];
                if (cursor.IsSingle)
                {
                    PieceTableIndex index = selectionOffset > 0 ? _pieceTable.GetIndexAtOffset(cursor.TableIndex, selectionOffset) : cursor.TableIndex;

                    _pieceTable.Insert(text, index);

                    selectionOffset += text.Length;
                    cursor.TextPosition = MoveTextPositionFrom(cursor.TableIndex, cursor.TextPosition, selectionOffset);
                    cursor.TableIndex = _pieceTable.GetIndexAtOffset(index, text.Length);

                    UIManager.Logger?.Debug("{ti}", cursor.TableIndex);
                }
            }

            _cachedString = null;
        }

        public void WriteText(char ch) => WriteText(new ReadOnlySpan<char>(ref ch));

        public void DeleteSelected(bool onlyDeleteSelections = false)
        {
            if (_pieceTable.IsEmpty)
            {
                for (int i = 0; i < _cursors.Count; i++)
                {
                    TextSelectionCursor cursor = _cursors[i];
                    if (i > 0 && !_cursors[i - 1].IsSingle)
                    {
                        ref TextSelectionCursor prevCursor = ref _cursors.AsSpan()[i - 1];
                        prevCursor.IsSingle = true;

                        _cursors.RemoveAt(i--);
                    }
                }
            }

            int selectionOffset = 0;
            for (int i = 0; i < _cursors.Count; i++)
            {
                TextSelectionCursor cursor = _cursors[i];
                if (cursor.IsSingle)
                {
                    if (i > 0 && !_cursors[i - 1].IsSingle)
                    {
                        ref TextSelectionCursor prevCursor = ref _cursors.AsSpan()[i - 1];

                        PieceTableIndex start = _pieceTable.GetIndexAtOffset(prevCursor.TableIndex, selectionOffset);
                        PieceTableIndex end = _pieceTable.GetIndexAtOffset(cursor.TableIndex, selectionOffset);

                        int comp = end.CompareTo(start);
                        if (comp == 0)
                        {
                            _cursors.RemoveAt(i--);
                            continue;
                        }
                        else if (comp < 0)
                            (start, end) = (end, start);

                        int diff = end.Offset - start.Offset;
                        Debug.Assert(diff > 0);

                        selectionOffset -= diff;

                        prevCursor.TextPosition = MoveTextPositionFrom(prevCursor.TableIndex, prevCursor.TextPosition, selectionOffset);
                        prevCursor.IsSingle = true;
                        prevCursor.TableIndex = start;

                        UIManager.Logger?.Debug("{ti}", start);

                        _pieceTable.Remove(diff, start);
                        _cachedString = null;

                        _cursors.RemoveAt(i--);
                    }
                    else
                    {
                        if (cursor.TableIndex.Offset == 0)
                            continue;

                        PieceTableIndex index = _pieceTable.GetIndexAtOffset(cursor.TableIndex, selectionOffset - 1);

                        if (!onlyDeleteSelections)
                        {
                            cursor.TextPosition = MoveTextPositionFrom(cursor.TableIndex, cursor.TextPosition, --selectionOffset);
                            cursor.TableIndex = _pieceTable.GetIndexAtOffset(index, 0);

                            UIManager.Logger?.Debug("{ti}", cursor.TableIndex);

                            _pieceTable.Remove(1, index);
                            _cachedString = null;
                        }

                        _cursors[i] = cursor;
                    }
                }
            }
        }

        public void SelectLeft()
        {

        }

        public void SelectRight()
        {

        }

        public void SelectUp()
        {

        }

        public void SelectDown()
        {

        }

        public void SetCursorFromHit(Vector2 hit)
        {
            if (_editState != TextEditActiveState.Default)
                return;
            if (_fontTypeData == null)
                return;

            KeyModifier mods = InputSystem.Keyboard.KeyModifiers;
            if (Flags.HasEither(mods, KeyModifier.Alt))
            {

            }
            else
            {
                if (_cursors.Count > 1)
                    _cursors.RemoveRange(1, _cursors.Count - 1);

                if (_cursors.Count > 0 && Flags.HasEither(mods, KeyModifier.Shift))
                {
                    (Int2 cursor, Vector2 pixel, PieceTableIndex index) = GetCursorFromHit(hit);

                    TextSelectionCursor selection = new TextSelectionCursor(cursor, pixel, index, true);
                    _cursors.Add(selection);

                    Span<TextSelectionCursor> span = _cursors.AsSpan();
                    span[0].IsSingle = false;
                }
                else
                {
                    (Int2 cursor, Vector2 pixel, PieceTableIndex index) = GetCursorFromHit(hit);

                    TextSelectionCursor selection = new TextSelectionCursor(cursor, pixel, index, true);
                    if (_cursors.Count == 0)
                        _cursors.Add(selection);
                    else
                        _cursors[0] = selection;
                }
            }
        }

        public void BeginCursorDrag(Vector2 hit)
        {
            if (_fontTypeData == null)
                return;
            if (_editState != TextEditActiveState.Default)
                return;

            _editState = TextEditActiveState.Dragging;

            (Int2 cursor, Vector2 pixel, PieceTableIndex index) = GetCursorFromHit(hit);

            if (_cursors.Count > 0)
            {
                _cursors.RemoveRange(1, _cursors.Count - 1);
                _cursors.AsSpan()[0].IsSingle = false;
            }
            else
                _cursors.Add(new TextSelectionCursor(cursor, pixel, index, false));

            _cursors.Add(new TextSelectionCursor(cursor, pixel, index, true));
        }

        public void UpdateCursorDrag(Vector2 hit)
        {
            if (_fontTypeData == null)
                return;
            if (_editState != TextEditActiveState.Dragging)
                return;

            if (_cursors.Count == 1 && _cursors[^1].IsSingle)
                _cursors.Add(default);

            (Int2 cursor, Vector2 pixel, PieceTableIndex index) = GetCursorFromHit(hit);
            _cursors[^1] = new TextSelectionCursor(cursor, pixel, index, true);
        }

        public void FinishCursorDrag()
        {
            if (_fontTypeData == null)
                return;
            if (_editState != TextEditActiveState.Dragging)
                return;

            _editState = TextEditActiveState.Default;
        }

        private (Int2 Cursor, Vector2 Pixel, PieceTableIndex Index) GetCursorFromHit(Vector2 hit)
        {
            Guard.IsNotNull(_fontTypeData);

            hit /= _fontSize * TextManager.PixelsPerEM;

            (PieceTableIndex startIndex, int line) = GetLineAt(hit.Y);

            if (hit.X < 0.0f)
            {
                return (new Int2(0, line), new Vector2(0.0f, line * _fontTypeData.Metrics.LineHeight), startIndex);
            }

            float xOffset = 0.0f;
            char lastChar = '\0';

            int letterIndex = 0;

            ReadOnlySpan<PieceRecord> records = _pieceTable.Records.Slice(startIndex.RecordIndex);
            for (int i = 0; i < records.Length; ++i)
            {
                ref readonly PieceRecord record = ref records.DangerousGetReferenceAt(i);

                ReadOnlySpan<char> tokens = record.Type == PieceType.Original ?
                    _pieceTable.OriginalBuffer.Slice(record.StartIndex, record.Length) :
                    _pieceTable.AddBuffer.Slice(record.StartIndex, record.Length);

                if (i == 0)
                    tokens = tokens[startIndex.LetterIndex..];

                for (int j = 0; j < tokens.Length; ++j)
                {
                    ref char c = ref tokens.DangerousGetReferenceAt(j);

                    if (c == '\n')
                    {
                        letterIndex += j;
                        goto EndLine;
                    }

                    if (char.IsControl(c))
                        continue;

                    UIGlyph glyph = _fontTypeData.RequestGlyph(c);
                    if (xOffset + glyph.Size.X * 0.5f > hit.X)
                    {
                        return (new Int2(j, line), new Vector2(xOffset, line * _fontTypeData.Metrics.LineHeight), _pieceTable.GetIndexAtOffset(startIndex, letterIndex + j));
                    }

                    xOffset += glyph.Advance;
                    lastChar = c;
                }

                letterIndex += tokens.Length;
            }

        EndLine:
            //if (lastChar != '\0')
            //{
            //    UIGlyph glyph = _fontStyle.RequestGlyph(lastChar);
            //    xOffset -= (glyph.Advance - glyph.Size.X);
            //}

            return (new Int2(letterIndex, line), new Vector2(xOffset, line * _fontTypeData.Metrics.LineHeight), _pieceTable.GetIndexAtOffset(startIndex, letterIndex));
        }

        private (PieceTableIndex StartIndex, int LineIndex) GetLineAt(float y)
        {
            Guard.IsNotNull(_fontTypeData);

            float lineHeight = _fontTypeData.Metrics.LineHeight;
            float lineY = _fontTypeData.Metrics.Height;

            PieceTableIndex previousLineIndex = PieceTableIndex.Zero;

            int lineIndex = 0;
            int letterIndex = 0;

            ReadOnlySpan<PieceRecord> records = _pieceTable.Records;
            for (int i = 0; i < records.Length; ++i)
            {
                ref readonly PieceRecord record = ref records.DangerousGetReferenceAt(i);

                ReadOnlySpan<char> tokens = record.Type == PieceType.Original ?
                    _pieceTable.OriginalBuffer.Slice(record.StartIndex, record.Length) :
                    _pieceTable.AddBuffer.Slice(record.StartIndex, record.Length);

                for (int j = 0; j < tokens.Length; ++j)
                {
                    ref char c = ref tokens.DangerousGetReferenceAt(j);
                    if (c == '\n')
                    {
                        PieceTableIndex tableIndex = j == tokens.Length - 1 ?
                            new PieceTableIndex(i + 1, 0, tokens.Length + letterIndex) :
                            new PieceTableIndex(i, j + 1, j + letterIndex + 1);

                        if (y < lineY)
                            return (previousLineIndex, lineIndex);

                        previousLineIndex = tableIndex;
                        lineY += lineHeight;
                        ++lineIndex;
                    }
                }

                letterIndex += tokens.Length;
            }

            return (previousLineIndex, lineIndex);
        }

        private Vector2 MoveTextPositionFrom(PieceTableIndex index, Vector2 textPos, int offset)
        {
            if (_fontTypeData == null || offset == 0)
                return textPos;

            ReadOnlySpan<char> original = _pieceTable.OriginalBuffer;
            ReadOnlySpan<char> add = _pieceTable.AddBuffer;

            if (offset > 0)
            {
                int finalOffset = offset;

                ReadOnlySpan<PieceRecord> records = _pieceTable.Records[index.RecordIndex..];
                for (int i = 0; i < records.Length; i++)
                {
                    ref readonly PieceRecord record = ref records[i];

                    ReadOnlySpan<char> letters = record.Type == PieceType.Original ? original.Slice(record.StartIndex, record.Length) : add.Slice(record.StartIndex, record.Length);
                    if (i == 0)
                        letters = letters[index.LetterIndex..];

                    for (int j = 0; j < letters.Length; j++)
                    {
                        ref readonly char c = ref letters.DangerousGetReferenceAt(j);

                        if (j == finalOffset)
                            break;

                        if (c == '\n')
                        {
                            textPos = new Vector2(0.0f, textPos.Y + _fontTypeData.Metrics.LineHeight);
                            continue;
                        }
                        else if (char.IsControl(c))
                            continue;

                        UIGlyph glyph = _fontTypeData.RequestGlyph(c);
                        textPos.X += glyph.Advance;
                    }

                    finalOffset -= letters.Length;
                }
            }
            else
            {
                offset = -offset;
                ReadOnlySpan<PieceRecord> records = _pieceTable.Records[..(index.RecordIndex + 1)];

                int lastIndex = records.Length - 1;
                for (int i = lastIndex; i >= 0; --i)
                {
                    ref readonly PieceRecord record = ref records[i];

                    ReadOnlySpan<char> letters = record.Type == PieceType.Original ? original.Slice(record.StartIndex, record.Length) : add.Slice(record.StartIndex, record.Length);
                    if (i == lastIndex)
                        letters = letters[..index.LetterIndex];

                    for (int j = letters.Length - 1; j >= 0; --j)
                    {
                        ref readonly char c = ref letters.DangerousGetReferenceAt(j);

                        if (offset == j)
                            break;

                        if (c == '\n')
                        {
                            textPos = new Vector2(0.0f, textPos.Y + _fontTypeData.Metrics.LineHeight);
                            continue;
                        }
                        else if (char.IsControl(c))
                            continue;

                        UIGlyph glyph = _fontTypeData.RequestGlyph(c);
                        textPos.X -= glyph.Advance;
                    }

                    offset -= letters.Length;
                }
            }

            return textPos;
        }

        public override string ToString()
        {
            if (_cachedString == null)
                _cachedString = _pieceTable.ToString();
            return _cachedString;
        }

        public TextEditActiveState CurrentState => _editState;

        public bool IsActive => _editState > TextEditActiveState.Inactive;

        internal StringPieceTable PieceTable => _pieceTable;

        public UIFontTypeData? FontTypeData { get => _fontTypeData; set => _fontTypeData = value; }
        public float FontSize { get => _fontSize; set => _fontSize = value; }

        public ReadOnlySpan<TextSelectionCursor> Cursors => _cursors.AsSpan();

        public string? CachedText => _cachedString;

        public event Action<bool>? FocusLost;
    }

    public record struct TextSelectionCursor(Int2 Cursor, Vector2 TextPosition, PieceTableIndex TableIndex, bool IsSingle);

    public enum TextEditActiveState : byte
    {
        Inactive = 0,
        Default,
        Dragging
    }
}
