using Editor.UI.Editing;
using Primary;
using Primary.Collections;
using Primary.Common;
using Primary.GUI.ImGui;
using Primary.Input;
using Primary.Input.Devices;
using Primary.IO;
using Primary.Rendering.Batching;
using System.Numerics;

namespace Editor.UI
{
    public sealed class TextEditManager
    {
        private TextEditState? _currentState;

        internal TextEditManager()
        {
            _currentState = null;

            InputSystem.Keyboard.TextInput += Keyboard_TextInput;
            InputSystem.Keyboard.KeyPressed += Keyboard_KeyPressed;
            InputSystem.Keyboard.KeyRepeated += Keyboard_KeyPressed;

            ImGuiManager imgui = Engine.GlobalSingleton.ImGuiManager;
            imgui.AddCallback(DrawDebugImGui);
        }

        private void Keyboard_TextInput(char text)
        {
            if (_currentState != null)
            {
                _currentState.WriteText(text);
            }
        }

        private void Keyboard_KeyPressed(KeyCode key)
        {
            if (_currentState != null)
            {
                switch (key)
                {
                    case KeyCode.Backspace: _currentState.DeleteSelected(); break;
                    case KeyCode.Delete: _currentState.DeleteSelected(true); break;
                    case KeyCode.Return: _currentState.WriteText('\n'); break;

                    case KeyCode.Tab: _currentState.WriteText('\t'); break;

                    case KeyCode.Left: _currentState.SelectLeft(); break;
                    case KeyCode.Right: _currentState.SelectRight(); break;
                    case KeyCode.Up: _currentState.SelectUp(); break;
                    case KeyCode.Down: _currentState.SelectDown(); break;

                    case KeyCode.V:
                        {
                            if (Flags.HasEither(InputSystem.Keyboard.KeyModifiers, KeyModifier.Control))
                            {
                                using RentedList<char> text = TrimUnwantedText(Clipboard.GetText());
                                if (!text.IsEmpty)
                                    _currentState.WriteText(text.AsSpan());
                            }

                            break;
                        }
                }
            }
        }

        public void SetActive(IElementOwner owner, TextEditState state)
        {
            _currentState?.Finish(false);

            state.Activate();
            _currentState = state;

            IWindowHost? host = owner.ParentHost;
            while (host!.HostWindow == null)
            {
                if (host is IWindowDockHost dockHost)
                    host = dockHost.ParentHost;
                else
                    break;
            }

            if (host.HostWindow != null)
            {
                host.HostWindow.StartTextInput();
            }
        }

        public void FinishEdit()
        {
            if (_currentState == null)
                return;

            _currentState.Finish(true);
            _currentState = null;
        }

        private void DrawDebugImGui()
        {
            if (_currentState != null)
            {
                if (IMGUI.BeginWindow("Tooltip_TEM", ImGuiWindowFlags.Tooltip))
                {
                    ImGuiWindowState windowState = IMGUI.CurrentWindow!;
                    ImGuiDrawList drawList = windowState.DrawList;

                    windowState.Position = new Vector2(InputSystem.Pointer.MousePosition.X + 15.0f, InputSystem.Pointer.MousePosition.Y + 10.0f);

                    if (_currentState != null)
                    {
                        IMGUI.Text($"String: org{_currentState.PieceTable.OriginalBuffer.Length} add{_currentState.PieceTable.AddBuffer.Length} rec{_currentState.PieceTable.Records.Length}");
                        IMGUI.Indent();
                        foreach (ref readonly PieceRecord record in _currentState.PieceTable.Records)
                        {
                            IMGUI.Text($"{(record.Type == PieceType.Add ? "Add" : "Original")} start{record.StartIndex} len{record.Length}");
                        }
                        IMGUI.Unindent();

                        IMGUI.Text($"Cursor: c{_currentState.Cursors.Length}");
                        IMGUI.Indent();
                        foreach (ref readonly TextSelectionCursor cursor in _currentState.Cursors)
                        {
                            IMGUI.Text($"c{cursor.Cursor} tp{cursor.TextPosition} single{cursor.IsSingle} table r{cursor.TableIndex.RecordIndex} l{cursor.TableIndex.LetterIndex} o{cursor.TableIndex.Offset}");
                        }
                        IMGUI.Unindent();

                        {
                            StringPieceTable pieceTable = _currentState.PieceTable;

                            ReadOnlySpan<char> original = pieceTable.OriginalBuffer;
                            ReadOnlySpan<char> add = pieceTable.AddBuffer;

                            ReadOnlySpan<PieceRecord> records = pieceTable.Records;

                            uint originalColor = 0xff00ffff;
                            uint addColor = 0xffff8000;

                            Vector2 cursorPos = windowState.CursorPos;
                            for (int i = 0; i < pieceTable.Records.Length; i++)
                            {
                                PieceRecord record = pieceTable.Records[i];

                                ReadOnlySpan<char> letters = record.Type == PieceType.Original ?
                                    original.Slice(record.StartIndex, record.Length) :
                                    add.Slice(record.StartIndex, record.Length);

                                int length = record.Length;
                                for (int j = 0; j < letters.Length; j++)
                                {
                                    char c = letters[j];
                                    if (c == '\n')
                                    {
                                        if (j > 0)
                                            drawList.DrawFilledRect(cursorPos, cursorPos + new Vector2(j, 1.0f), record.Type == PieceType.Original ? originalColor : addColor);

                                        drawList.DrawFilledRect(cursorPos + new Vector2(j + 1, 0.0f), cursorPos + new Vector2(j + 2, 0.0f));

                                        cursorPos = new Vector2(windowState.CursorPos.X, cursorPos.Y + 1.0f);
                                        length -= j;
                                    }
                                }

                                if (length > 0)
                                {
                                    drawList.DrawFilledRect(cursorPos, cursorPos + new Vector2(length, 1.0f), record.Type == PieceType.Original ? originalColor : addColor);
                                    cursorPos.X += length + 1;
                                }
                            }

                            cursorPos.X += 2.0f;
                            IMGUI.AddItemSize(cursorPos - windowState.CursorPos);
                        }
                    }

                    IMGUI.EndWindow();
                }

                if (IMGUI.BeginWindow("Piece preview", ImGuiWindowFlags.AlwaysResize))
                {
                    ImGuiWindowState windowState = IMGUI.CurrentWindow!;
                    ImGuiDrawList drawList = windowState.DrawList;

                    StringPieceTable pieceTable = _currentState!.PieceTable;

                    ReadOnlySpan<char> original = pieceTable.OriginalBuffer;
                    ReadOnlySpan<char> add = pieceTable.AddBuffer;

                    ReadOnlySpan<PieceRecord> records = pieceTable.Records;

                    uint originalColor = 0x4000ffff;
                    uint addColor = 0x80ff8000;

                    float maxX = 0.0f;

                    Vector2 cursorPos = windowState.CursorPos;
                    for (int i = 0; i < pieceTable.Records.Length; i++)
                    {
                        PieceRecord record = pieceTable.Records[i];

                        ReadOnlySpan<char> letters = record.Type == PieceType.Original ?
                            original.Slice(record.StartIndex, record.Length) :
                            add.Slice(record.StartIndex, record.Length);

                        int length = record.Length;
                        int offset = 0;

                        for (int j = 0; j < letters.Length; j++)
                        {
                            char c = letters[j];
                            if (c == '\n')
                            {
                                if (j > 0)
                                {
                                    ReadOnlySpan<char> text = letters.Slice(offset, j - offset);
                                    Vector2 size = IMGUI.CalculateTextSize(text);

                                    drawList.DrawFilledRect(cursorPos, cursorPos + new Vector2(size.X, size.Y), record.Type == PieceType.Original ? originalColor : addColor);
                                    drawList.DrawText(cursorPos, text);

                                    cursorPos.X += size.X;
                                }

                                drawList.DrawFilledTriangle(cursorPos + new Vector2(3.0f, 9.0f), cursorPos + new Vector2(11.0f, 9.0f), cursorPos + new Vector2(11.0f, 1.0f));

                                maxX = Math.Max(maxX, cursorPos.X);
                                cursorPos = new Vector2(windowState.CursorPos.X, cursorPos.Y + ImGuiFont.FontVisualHeight + 1.0f);

                                offset = j + 1;
                            }
                        }

                        length -= offset;
                        if (length > 0)
                        {
                            ReadOnlySpan<char> text = letters.Slice(letters.Length - length, length);
                            Vector2 size = IMGUI.CalculateTextSize(text);

                            drawList.DrawFilledRect(cursorPos, cursorPos + new Vector2(size.X, size.Y), record.Type == PieceType.Original ? originalColor : addColor);
                            drawList.DrawText(cursorPos, text);

                            cursorPos.X += size.X + 2.0f;
                        }
                    }

                    cursorPos.X += 8.0f;
                    cursorPos.Y += ImGuiFont.FontVisualHeight + 1.0f;

                    IMGUI.AddItemSize(new Vector2(Math.Max(cursorPos.X, maxX), cursorPos.Y) - windowState.CursorPos);
                    IMGUI.EndWindow();
                }
            }
        }

        private static RentedList<char> TrimUnwantedText(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return new RentedList<char>();

            RentedList<char> list = new RentedList<char>();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!char.IsControl(c))
                    list.Add(c);
            }

            return list;
        }
    }
}
