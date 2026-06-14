using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Editing;
using Editor.UI.Helpers;
using Editor.UI.Interaction;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Data;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Timing;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("TextField"), StyleableStates("Normal", "Hovered", "Pressed", "Active")]
    public class UITextField : UIScrollView
    {
        private bool _isReadOnly;
        private bool _isMultiline;
        private bool _commitWhenFocusLost;

        private int _maxLength;

        private StringPieceTable _text;
        private string _placeholderText;

        private UIFontAsset? _font;
        private FontStyle _style;
        private FontWeight _weight;

        private UIColor _textColor;
        private UIColor _placeholderColor;
        private float _fontSize;

        private TextEditState _textEditState;

        public UITextField()
        {
            _isReadOnly = false;
            _isMultiline = false;
            _commitWhenFocusLost = true;

            _maxLength = int.MaxValue;

            _text = new StringPieceTable(string.Empty);
            _placeholderText = string.Empty;

            _font = null;
            _style = FontStyle.Normal;
            _weight = FontWeight.Normal;

            _textColor = Color.White;
            _fontSize = 1.0f;

            _textEditState = new TextEditState(_text);
            _textEditState.FocusLost += EditFocusLost;
        }

        public UITextField(UIElement parent) : this()
        {
            SetParent(parent);
        }

        protected virtual bool OnTextCommited(ref string text) => true;

        protected override void DestroySelf()
        {
            if (_textEditState.IsActive)
                UIManager.Instance.TextEditManager.FinishEdit();

            _textEditState.FocusLost -= EditFocusLost;

            base.DestroySelf();
        }

        protected void SetNewText(string text)
        {
            _text.ResetForNewText(text);
            _textEditState.ClearNewText(text);
        }

        private void EditFocusLost(bool isEditingFinished)
        {
            if (!isEditingFinished)
            {
                if (_commitWhenFocusLost)
                    CommitText();

                SetState("Active", false);
            }
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            base.DrawVisual(painter);

            if (_font != null)
            {
                if (_font != null)
                {
                    TextBuilder text = new TextBuilder();
                    text.SetMaxExtents(_viewSize);

                    UIFontTypeData typeData = _font.FindStyle(_style, _weight)!;

                    if (_textEditState.IsActive)
                    {
                        ReadOnlySpan<TextSelectionCursor> cursors = _textEditState.Cursors;
                        for (int i = 0; i < cursors.Length; ++i)
                        {
                            ref readonly TextSelectionCursor cursor = ref cursors[i];
                            if (cursor.IsSingle && i > 0 && !cursors[i - 1].IsSingle)
                            {
                                ref readonly TextSelectionCursor prev = ref cursors[i - 1];

                                if (prev.TableIndex.Offset < cursor.TableIndex.Offset)
                                    DrawHighlightRange(painter, prev.TableIndex, cursor.TableIndex, prev.TextPosition);
                                else
                                    DrawHighlightRange(painter, cursor.TableIndex, prev.TableIndex, cursor.TextPosition);
                            }
                        }
                    }

                    if (!_text.IsEmpty)
                    {
                        if (_textEditState.CachedText != null)
                            painter.DrawText(_viewCoordinates.Minimum, UIPaint.FromColor(_textColor), text, typeData, _fontSize, _textEditState.CachedText);
                        else
                            painter.DrawText(_viewCoordinates.Minimum, UIPaint.FromColor(_textColor), text, typeData, _fontSize, new InternalJaggedString(_text));
                    }

                    if (_textEditState.IsActive && (Time.TimestampForActiveFrame / 4500000) % 2 == 0)
                    {
                        float scale = _fontSize * TextManager.PixelsPerEM;

                        ReadOnlySpan<TextSelectionCursor> cursors = _textEditState.Cursors;
                        for (int i = 0; i < cursors.Length; ++i)
                        {
                            ref readonly TextSelectionCursor cursor = ref cursors[i];

                            Vector2 min = cursor.TextPosition * scale + _viewCoordinates.Minimum;
                            painter.DrawRect(new Boundaries(
                                new Vector2(min.X, min.Y + typeData.Metrics.Descender * scale),
                                min + new Vector2(1.0f, typeData.Metrics.LineHeight * scale)), UIPaint.FromColor(Color.White));
                        }
                    }
                }
            }

            return true;
        }

        private void DrawHighlightRange(UIPainterContext painter, PieceTableIndex from, PieceTableIndex to, Vector2 textPos)
        {
            if (_font == null)
                return;

            UIFontTypeData typeData = _font.FindStyle(_style, _weight)!;

            UIPaint paint = UIPaint.FromColor(new Color(0.0f, 0.0f, 1.0f, 0.2f));
            float scale = TextManager.PixelsPerEM * _fontSize;

            Vector2 startPos = textPos;

            ReadOnlySpan<PieceRecord> records = _text.Records;

            ReadOnlySpan<char> original = _text.OriginalBuffer;
            ReadOnlySpan<char> add = _text.AddBuffer;

            int recordIndex = from.RecordIndex;

            for (int letter = from.Offset; letter < to.Offset && recordIndex < records.Length;)
            {
                ref readonly PieceRecord record = ref records[recordIndex];

                int start = recordIndex == from.RecordIndex ? from.LetterIndex : 0;
                int length = (recordIndex == to.RecordIndex ? to.LetterIndex : record.Length) - start;

                start += record.StartIndex;

                ReadOnlySpan<char> text = record.Type == PieceType.Original ? original.Slice(start, length) : add.Slice(start, length);

                for (int j = 0; j < text.Length; j++)
                {
                    ref char c = ref text.DangerousGetReferenceAt(j);

                    if (c == '\n')
                    {
                        if (textPos != startPos && textPos.X > float.Epsilon)
                        {
                            painter.DrawRect(new Boundaries(startPos, new Vector2(textPos.X, textPos.Y + typeData.Metrics.LineHeight)) * scale + _viewCoordinates.Minimum, paint);
                        }

                        textPos = new Vector2(0.0f, textPos.Y + typeData.Metrics.LineHeight);
                        startPos = textPos;

                        continue;
                    }
                    else if (char.IsControl(c))
                        continue;

                    UIGlyph glyph = typeData.RequestGlyph(c);
                    textPos.X += glyph.Advance;
                }

                ++recordIndex;
                letter += length;
            }

            if (textPos != startPos && textPos.X > float.Epsilon)
            {
                painter.DrawRect(new Boundaries(startPos, new Vector2(textPos.X, textPos.Y + typeData.Metrics.LineHeight)) * scale + _viewCoordinates.Minimum, paint);
            }
        }

        public override void HandleEvent(ref readonly UIEvent @event)
        {
            switch (@event.Type)
            {
                case UIEventType.MouseEnter: SetState("Hovered", true); break;
                case UIEventType.MouseLeave: SetState("Hovered", false); break;
                case UIEventType.MouseDown:
                    {
                        if (@event.Mouse.Button == MouseButton.Left)
                        {
                            SetState("Pressed", true);

                            _textEditState.FontTypeData = _font?.FindStyle(_style, _weight);
                            _textEditState.SetCursorFromHit(@event.Mouse.Position - _viewCoordinates.Minimum);
                        }
                        break;
                    }
                case UIEventType.MouseUp:
                    {
                        if (@event.Mouse.Button == MouseButton.Left)
                            SetState("Pressed", false);
                        break;
                    }
                case UIEventType.MouseActivate:
                    {
                        if (@event.Mouse.Button == MouseButton.Left)
                        {
                            if (!_textEditState.IsActive)
                            {
                                SetState("Active", true);

                                UIManager.Instance.TextEditManager.SetActive(WindowOwner!, _textEditState);

                                _textEditState.FontTypeData = _font?.FindStyle(_style, _weight);
                                _textEditState.SetCursorFromHit(@event.Mouse.Position - _viewCoordinates.Minimum);
                            }
                        }

                        break;
                    }
                case UIEventType.MouseFocusLost:
                    {
                        if (_textEditState.IsActive && @event.Mouse.Button == MouseButton.Left)
                        {
                            SetState("Active", false);
                            UIManager.Instance.TextEditManager.FinishEdit();

                            if (_commitWhenFocusLost)
                                CommitText();
                        }

                        break;
                    }
                case UIEventType.DragBegin:
                    {
                        if (!_textEditState.IsActive)
                        {
                            SetState("Active", true);
                            UIManager.Instance.TextEditManager.SetActive(WindowOwner!, _textEditState);

                        }

                        Vector2 hit = @event.Drag.Position - @event.Drag.Delta - _viewCoordinates.Minimum;
                        _textEditState.FontTypeData = _font?.FindStyle(_style, _weight);
                        _textEditState.BeginCursorDrag(hit);

                        break;
                    }
                case UIEventType.DragUpdate:
                    {
                        if (_textEditState.IsActive)
                        {
                            _textEditState.FontTypeData = _font?.FindStyle(_style, _weight);
                            _textEditState.UpdateCursorDrag(@event.Drag.Position - _viewCoordinates.Minimum);
                        }

                        break;
                    }
                case UIEventType.DragEnd:
                    {
                        if (_textEditState.IsActive)
                        {
                            _textEditState.FontTypeData = _font?.FindStyle(_style, _weight);
                            _textEditState.FinishCursorDrag();
                        }

                        break;
                    }
            }
        }

        /// <summary>Commits the text in the internal piece table and clears all history</summary>
        public void CommitText()
        {
            string text = _textEditState.ToString();
            if (OnTextCommited(ref text))
                SetNewText(text);
        }

        #region Properties
        [EditableProperty(nameof(_isReadOnly))] public bool IsReadOnly { get => _isReadOnly; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_isMultiline), UIStateFlags.InvalidVisual)] public bool IsMultiline { get => _isMultiline; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_commitWhenFocusLost))] public bool CommitWhenFocusLost { get => _commitWhenFocusLost; set => SetEditableProperty(value); }

        [EditableProperty(nameof(_maxLength))] public int MaxLength { get => _maxLength; set => SetEditableProperty(value); }

        /// <summary>get: Not thread-safe</summary>
        [EditableProperty("", UIStateFlags.InvalidVisual)] public string Text { get => _textEditState.ToString(); set => SetNewText(value); }
        [EditableProperty(nameof(_placeholderText), UIStateFlags.InvalidVisual)] public string PlaceholderText { get => _placeholderText; set => _placeholderText = value; }

        [StyleableProperty(nameof(_font), UIStateFlags.InvalidVisual)] public UIFontAsset? Font { get => _font; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_style), UIStateFlags.InvalidVisual)] public FontStyle FontStyle { get => _style; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_weight), UIStateFlags.InvalidVisual)] public FontWeight FontWeight { get => _weight; set => SetStyleProperty(value); }

        [StyleableProperty(nameof(_textColor), UIStateFlags.InvalidVisual)] public UIColor TextColor { get => _textColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_placeholderColor), UIStateFlags.InvalidVisual)] public UIColor PlaceholderColor { get => _placeholderColor; set => SetStyleProperty(value); }
        [StyleableProperty(nameof(_fontSize), UIStateFlags.InvalidLayout)] public float FontSize { get => _fontSize; set => SetStyleProperty(value); }
        #endregion
        #region Events

        #endregion

        private enum ExtraUIStateFlags : byte
        {
            InvalidStyle = 1 << 5,
        }

        private record struct InternalJaggedString(StringPieceTable PieceTable) : IJaggedString
        {
            private int _recordIndex = 0;

            public ReadOnlySpan<char> MoveNext()
            {
                if (_recordIndex == PieceTable.Records.Length)
                    return ReadOnlySpan<char>.Empty;

                ref readonly PieceRecord record = ref PieceTable.Records[_recordIndex++];
                if (record.Type == PieceType.Add)
                    return PieceTable.AddBuffer.Slice(record.StartIndex, record.Length);
                else
                    return PieceTable.OriginalBuffer.Slice(record.StartIndex, record.Length);
            }

            public int Length
            {
                get
                {
                    int count = 0;
                    foreach (ref readonly PieceRecord record in PieceTable.Records)
                    {
                        count += record.Length;
                    }

                    return count;
                }
            }
        }
    }
}
