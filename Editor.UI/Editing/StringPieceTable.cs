using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Data;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Editor.UI.Editing
{
    public sealed class StringPieceTable
    {
        private int _size;

        private ImmutableArray<char> _originalBuffer;
        private List<char> _addBuffer;

        private List<PieceRecord> _records;

        private PieceTableIndex _cursor;

        public StringPieceTable(ReadOnlySpan<char> source)
        {
            _size = source.Length;

            _originalBuffer = source.ToImmutableArray();
            _addBuffer = new List<char>();

            _records = [new PieceRecord(PieceType.Original, 0, source.Length)];

            _cursor = new PieceTableIndex(0, 0);
        }

        private bool IsTableIndexValid(PieceTableIndex index)
        {
            return (uint)index.RecordIndex < _records.Count && index.LetterIndex <= _records[index.RecordIndex].Length;
        }

        private int WriteIntoAddBuffer(ReadOnlySpan<char> text)
        {
            int startIndex = _addBuffer.Count;

            _addBuffer.AddRange(text);
            return startIndex;
        }

        private int SplitRecord(PieceTableIndex index)
        {
            PieceRecord toSplit = _records[index.RecordIndex];

            if (index.LetterIndex == 0)
            {
                return index.RecordIndex;
            }
            else
            {
                PieceRecord left = new PieceRecord(toSplit.Type, toSplit.StartIndex, index.LetterIndex);
                PieceRecord right = new PieceRecord(toSplit.Type, toSplit.StartIndex + index.LetterIndex, toSplit.Length - index.LetterIndex);

                if (left.Length == 0)
                {
                    _records[index.RecordIndex] = right;

                    return index.RecordIndex;
                }
                else if (right.Length == 0)
                {
                    _records[index.RecordIndex] = left;

                    return index.RecordIndex + 1;
                }
                else
                {
                    _records[index.RecordIndex] = left;
                    _records.Insert(index.RecordIndex + 1, right);

                    return index.RecordIndex + 1;
                }
            }
        }

        public PieceTableIndex GetCursor(SeekOrigin origin, int letterIndex)
        {
            if (letterIndex < 0)
                throw new NotSupportedException("Cannot seek get cursor backwards yet!");

            Span<PieceRecord> records = _records.AsSpan();
            if (records.IsEmpty)
            {
                return new PieceTableIndex(0, 0);
            }
            else
            {
                int currentLetterIndex = 0;
                if (origin == SeekOrigin.Current)
                {
                    currentLetterIndex = _cursor.Offset - _cursor.LetterIndex;
                    letterIndex += _cursor.Offset;
                }
                else if (origin == SeekOrigin.End)
                    throw new NotImplementedException();

                for (int i = origin == SeekOrigin.Current ? _cursor.RecordIndex : 0; i < _records.Count; ++i)
                {
                    ref PieceRecord record = ref records[i];

                    int nextLetterIndex = currentLetterIndex + record.Length;
                    if (letterIndex <= nextLetterIndex)
                    {
                        int localLetterIndex = letterIndex - currentLetterIndex;
                        return new PieceTableIndex(i, localLetterIndex, currentLetterIndex + localLetterIndex);
                    }

                    currentLetterIndex = nextLetterIndex;
                }

                ref PieceRecord lastRecord = ref records[records.Length - 1];
                return new PieceTableIndex(_records.Count - 1, lastRecord.Length - 1);
            }
        }

        public void ResetForNewText(ReadOnlySpan<char> text)
        {
            _size = text.Length;

            _originalBuffer = text.ToImmutableArray();

            _addBuffer.Clear();
            _addBuffer.TrimExcess();

            _records.Clear();
            _records.Add(new PieceRecord(PieceType.Original, 0, text.Length));

            _cursor = new PieceTableIndex(0, 0);
        }

        public void Seek(int letterIndex)
        {
            Guard.IsGreaterThanOrEqualTo(letterIndex, 0, "Cannot seek to negative letter index");
            _cursor = GetCursor(SeekOrigin.Begin, letterIndex);
        }

        /// <summary>Writes text without inserting into the buffer</summary>
        public void Write(ReadOnlySpan<char> text, PieceTableIndex index)
        {
            Guard.IsTrue(IsTableIndexValid(index));

            if (text.IsEmpty)
                return;

            _size += Math.Max((text.Length + index.Offset) - _size, 0);

            int startIndex = WriteIntoAddBuffer(text);
            int splitIndex = -1;

            int offsetWithinFirstRecord = _records[index.RecordIndex].Length - index.LetterIndex;
            int letterIndex = index.Offset - offsetWithinFirstRecord;

            for (int i = index.RecordIndex; i < _records.Count; ++i)
            {
                PieceRecord record = _records[index.RecordIndex];

                // is entire piece within current buffer
                if (letterIndex >= index.Offset && letterIndex + record.Length <= text.Length)
                {
                    if (splitIndex == -1)
                        splitIndex = i;
                    _records.RemoveAt(i--);
                }
                else if (letterIndex >= index.Offset)
                {
                    if (splitIndex == -1)
                        splitIndex = i;

                    int newLength = record.Length - (letterIndex - index.Offset);
                    _records[i] = new PieceRecord(record.Type, record.StartIndex + text.Length, newLength);

                    break;
                }
                else
                {
                    Debug.Assert(i == index.RecordIndex);

                    splitIndex = i + 1;
                    _records[i] = new PieceRecord(record.Type, record.StartIndex, offsetWithinFirstRecord);
                }
            }

            Debug.Assert(splitIndex != -1);

            _records.Insert(splitIndex, new PieceRecord(PieceType.Add, startIndex, text.Length));
        }

        /// <summary>Inserts text into the buffer</summary>
        public void Insert(ReadOnlySpan<char> text, PieceTableIndex index)
        {
            Guard.IsTrue(IsTableIndexValid(index));

            if (text.IsEmpty)
                return;

            _size += text.Length;

            int startIndex = WriteIntoAddBuffer(text);

            PieceRecord record = _records[index.RecordIndex];
            if (record.Length == 0)
            {
                _records[index.RecordIndex] = new PieceRecord(PieceType.Add, startIndex, text.Length);
                return;
            }

            if (record.Type == PieceType.Add)
            {
                if (index.LetterIndex == record.Length && record.StartIndex + record.Length == startIndex)
                {
                    _records[index.RecordIndex] = new PieceRecord(PieceType.Add, record.StartIndex, record.Length + text.Length);
                }
                else
                {
                    int splitIndex = SplitRecord(index);
                    _records.Insert(splitIndex, new PieceRecord(PieceType.Add, startIndex, text.Length));
                }
            }
            else
            {
                int splitIndex = SplitRecord(index);
                _records.Insert(splitIndex, new PieceRecord(PieceType.Add, startIndex, text.Length));
            }
        }

        /// <summary>Removes text from the buffer</summary>
        public void Remove(int length, PieceTableIndex index)
        {
            if (index.Offset == 0 && length == _size)
            {
                _records.Clear();
                _records.Add(new PieceRecord(PieceType.Add, _addBuffer.Count, 0));

                return;
            }

            ArgumentOutOfRangeException.ThrowIfNegative(length);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index.Offset + length, _size);

            Guard.IsTrue(IsTableIndexValid(index));

            {
                PieceRecord currentRecord = _records[index.RecordIndex];
                if (index.LetterIndex + length <= currentRecord.Length)
                {
                    _size -= length;

                    if (index.LetterIndex == 0)
                    {
                        if (currentRecord.Length == length)
                            _records.RemoveAt(index.RecordIndex);
                        else
                            _records[index.RecordIndex] = new PieceRecord(currentRecord.Type, currentRecord.StartIndex + length, currentRecord.Length - length);
                    }
                    else if (index.LetterIndex == currentRecord.Length - 1)
                    {
                        _records[index.RecordIndex] = new PieceRecord(currentRecord.Type, currentRecord.StartIndex, currentRecord.Length - length);
                    }
                    else
                    {
                        int offsetIndex = index.LetterIndex + length;

                        PieceRecord left = new PieceRecord(currentRecord.Type, currentRecord.StartIndex, index.LetterIndex);
                        PieceRecord right = new PieceRecord(currentRecord.Type, currentRecord.StartIndex + offsetIndex, currentRecord.Length - offsetIndex);

                        _records[index.RecordIndex] = left;

                        if (right.Length > 0)
                            _records.Insert(index.RecordIndex + 1, right);
                    }

                    return;
                }
            }

            int letterIndex = GetLetterIndexAtStart(index);
            int endLetterIndex = index.Offset + length;

            _size -= length;

            IndexRange cutRange = IndexRange.Empty;

            for (int i = index.RecordIndex; i < _records.Count; ++i)
            {
                PieceRecord record = _records[i];

                int currentLetterIndex = i == index.RecordIndex ? index.Offset : letterIndex;
                int nextLetterIndex = letterIndex + record.Length;

                bool isStartConsumed = i == index.RecordIndex ? currentLetterIndex == letterIndex : true;
                bool isEndConsumed = endLetterIndex >= nextLetterIndex;

                Debug.Assert(isStartConsumed || isEndConsumed);

                if (isStartConsumed && isEndConsumed)
                {
                    if (cutRange.IsEmpty)
                        cutRange = new IndexRange(i, i + 1);
                    else
                        cutRange.End = i + 1;
                }
                else if (isEndConsumed)
                {
                    _records[i] = new PieceRecord(record.Type, record.StartIndex, index.LetterIndex);
                }
                else
                {
                    int offset = endLetterIndex - currentLetterIndex;
                    _records[i] = new PieceRecord(record.Type, record.StartIndex + offset, record.Length - offset);
                }

                if (nextLetterIndex >= endLetterIndex)
                    break;

                letterIndex = nextLetterIndex;
            }

            if (!cutRange.IsEmpty)
            {
                UIManager.Logger?.Information("{x}", cutRange);
                _records.RemoveRange(cutRange.Start, cutRange.Length);
            }
        }

        /// <inheritdoc cref="Write(ReadOnlySpan{char}, PieceTableIndex)" />
        public void Write(ReadOnlySpan<char> text)
        {
            Write(text, _cursor);
            _cursor = GetCursor(SeekOrigin.Current, text.Length);
        }
        /// <inheritdoc cref="Insert(ReadOnlySpan{char}, PieceTableIndex)" />
        public void Insert(ReadOnlySpan<char> text)
        {
            Insert(text, _cursor);
            _cursor = GetCursor(SeekOrigin.Current, text.Length);
        }
        /// <inheritdoc cref="Insert(ReadOnlySpan{char}, PieceTableIndex)" />
        public void Remove(int length)
        {
            Remove(length, _cursor);
            _cursor = GetCursor(SeekOrigin.Current, -length);
        }

        public PieceTableIndex GetIndexAtOffset(PieceTableIndex index, int offset)
        {
            if (_records.Count == 0)
                return PieceTableIndex.Zero;

            // clamp input
            if (index.RecordIndex < 0)
                index = PieceTableIndex.Zero;
            else if (index.RecordIndex >= _records.Count)
                index = new PieceTableIndex(_records.Count - 1, _records[_records.Count - 1].Length, _size);

            if (offset == 0)
                return index;

            if (offset > 0)
            {
                if (index.RecordIndex == _records.Count - 1)
                {
                    offset = Math.Min(offset, _records[index.RecordIndex].Length - index.LetterIndex);
                    return new PieceTableIndex(index.RecordIndex, index.LetterIndex + offset, index.Offset + offset);
                }
                else if (index.LetterIndex + offset < _records[index.RecordIndex].Length)
                {
                    return new PieceTableIndex(index.RecordIndex, index.LetterIndex + offset, index.Offset + offset);
                }
                else
                {
                    int globalOffset = index.Offset + offset;

                    offset -= _records[index.RecordIndex].Length - index.LetterIndex;

                    // the next is the first letter in a new record
                    if (offset == 0)
                        return new PieceTableIndex(index.RecordIndex + 1, 0, globalOffset);

                    for (int i = index.RecordIndex + 1; i < _records.Count; ++i)
                    {
                        PieceRecord record = _records[i];

                        if (record.Length > offset)
                        {
                            return new PieceTableIndex(i, offset, globalOffset);
                        }

                        offset -= record.Length;
                    }

                    PieceRecord lastRecord = _records[_records.Count - 1];
                    return new PieceTableIndex(_records.Count - 1, lastRecord.Length, _size);
                }
            }
            else
            {
                if (index.LetterIndex + offset >= 0)
                {
                    return new PieceTableIndex(index.RecordIndex, index.LetterIndex + offset, index.Offset + offset);
                }
                else
                {
                    int globalOffset = index.Offset + offset;
                    if (globalOffset <= 0)
                        return PieceTableIndex.Zero;

                    offset = -offset;

                    for (int i = index.RecordIndex - 1; i >= 0; --i)
                    {
                        PieceRecord record = _records[i];

                        if (record.Length > offset)
                        {
                            return new PieceTableIndex(i, record.Length - offset, globalOffset);
                        }

                        offset -= record.Length;
                    }
                }
            }

            throw new UnreachableException();
        }

        public int GetLetterIndexAtStart(PieceTableIndex index)
        {
            Guard.IsTrue(IsTableIndexValid(index));

            if (index.Offset == 0)
                return 0;

            //int offsetWithinFirstRecord = _records[index.RecordIndex].Length - (_records[index.RecordIndex].Length - index.LetterIndex);
            return index.Offset - index.LetterIndex;
        }

        public override string ToString()
        {
            if (_records.Count == 0)
                return string.Empty;
            else if (_records.Count == 1)
            {
                PieceRecord firstRecord = _records[0];
                if (firstRecord.Length == 0)
                    return string.Empty;

                return (firstRecord.Type == PieceType.Original ?
                    _originalBuffer.AsSpan(firstRecord.StartIndex, firstRecord.Length) :
                    _addBuffer.AsSpan().Slice(firstRecord.StartIndex, firstRecord.Length)).ToString();
            }

            FastStringBuilder sb = new FastStringBuilder();

            for (int i = 0; i < _records.Count; i++)
            {
                PieceRecord record = _records[i];
                sb.Append(record.Type == PieceType.Original ?
                    _originalBuffer.AsSpan(record.StartIndex, record.Length) :
                    _addBuffer.AsSpan().Slice(record.StartIndex, record.Length));
            }

            return sb.ToString();
        }

        public ReadOnlySpan<char> OriginalBuffer => _originalBuffer.AsSpan();
        public ReadOnlySpan<char> AddBuffer => _addBuffer.AsSpan();

        public ReadOnlySpan<PieceRecord> Records => _records.AsSpan();

        public PieceTableIndex Cursor => _cursor;

        public bool IsEmpty => _originalBuffer.IsEmpty && _addBuffer.Count == 0;
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly record struct PieceTableIndex : IComparable<PieceTableIndex>
    {
        [FieldOffset(0)] public readonly long Index;
        [FieldOffset(8)] public readonly int Offset;

        [FieldOffset(0)] public readonly int RecordIndex;
        [FieldOffset(4)] public readonly int LetterIndex;

        public PieceTableIndex(long index, int offset)
        {
            Index = index;
            Offset = offset;
        }

        public PieceTableIndex(int recordIndex, int letterIndex, int offset)
        {
            RecordIndex = recordIndex;
            LetterIndex = letterIndex;
            Offset = offset;
        }

        public int CompareTo(PieceTableIndex other) => Offset.CompareTo(other.Offset);

        public static readonly PieceTableIndex Zero = new PieceTableIndex(0, 0);
    }

    public readonly record struct PieceRecord(PieceType Type, int StartIndex, int Length);

    public enum PieceType : byte
    {
        Original = 0,
        Add,
    }
}
