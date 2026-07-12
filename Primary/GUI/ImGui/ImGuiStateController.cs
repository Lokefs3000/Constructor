using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Polling;
using SDL;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Primary.GUI.ImGui
{
    public sealed class ImGuiStateController : IEventHandler
    {
        private readonly ImGuiContext _context;

        private int[] _idStack;
        private int _idStackIndex;

        private Vector2 _mouseDragPosition;
        private Vector2 _itemDragPosition;

        private List<ImGuiWindowState> _windowStates;

        private int _currentWindowId;
        private ImGuiWindowState? _currentWindow;

        private int _activeWindowId;
        private bool _hasFocusAppeared;
        private int _tooltipWindowId;

        private int _hotId;
        private int _activeId;
        private int _dragId;

        private int _keyboardId;
        private Boundaries _keyboardInputRect;
        private bool _keyboardFocusThisFrame;

        private ImGuiTextState _textState;
        private Queue<(SDL_Keycode Key, SDL_Keymod Mod)> _keysPressedSinceLast;
        private char _lastInputChar;

        private (bool IsDragging, Vector2 StartPos, Vector2 Delta)[] _isDragging;

        internal ImGuiStateController(ImGuiContext context)
        {
            _context = context;

            _idStack = new int[8];
            _idStackIndex = 0;

            _windowStates = new List<ImGuiWindowState>();

            _currentWindowId = InvalidId;
            _currentWindow = null;

            _activeWindowId = InvalidId;
            _hasFocusAppeared = false;

            _hotId = InvalidId;
            _activeId = InvalidId;
            _dragId = InvalidId;

            _keyboardId = InvalidId;
            _keyboardInputRect = Boundaries.Zero;
            _keyboardFocusThisFrame = false;

            _textState = new ImGuiTextState();
            _keysPressedSinceLast = new Queue<(SDL_Keycode Key, SDL_Keymod Mod)>();
            _lastInputChar = '\uffff';

            _isDragging = new (bool IsDragging, Vector2 StartPos, Vector2 Delta)[3];
        }

        internal void Prepare()
        {
            _idStackIndex = 0;

            _currentWindowId = InvalidId;
            _currentWindow = null;

            if (!_hasFocusAppeared)
                _activeWindowId = InvalidId;
            _hasFocusAppeared = false;
            _tooltipWindowId = InvalidId;

            _hotId = InvalidId;
            _keyboardFocusThisFrame = false;

            for (int i = 0; i < _isDragging.Length; i++)
            {
                MouseButton button = (MouseButton)i;
                if (!_isDragging[i].IsDragging)
                {
                    if (InputSystem.Pointer.IsButtonHeld(button))
                    {
                        if (InputSystem.Pointer.IsButtonPressed(button))
                            _isDragging[i].StartPos = InputSystem.Pointer.MousePosition;

                        if (Vector2.Distance(_isDragging[i].StartPos, InputSystem.Pointer.MousePosition) > 2.0f)
                        {
                            _isDragging[i].IsDragging = true;
                        }
                    }
                }
                else
                {
                    if (!InputSystem.Pointer.IsButtonHeld(button))
                    {
                        _isDragging[i].IsDragging = false;
                        _isDragging[i].Delta = Vector2.Zero;
                    }
                    else
                    {
                        _isDragging[i].Delta = InputSystem.Pointer.MousePosition - _isDragging[i].StartPos;
                    }
                }
            }
        }

        internal void Finish()
        {
            if (!InputSystem.Pointer.IsButtonHeld(MouseButton.Left))
            {
                _activeId = InvalidId;
            }
            else
            {
                if (_activeId == InvalidId)
                    _activeId = TransientId;
            }

            if (!_keyboardFocusThisFrame)
            {
                if (_keyboardId > InvalidId && InputSystem.Keyboard.HasKeyboardFocus)
                    InputSystem.Keyboard.EndKeyboardFocus();

                _keyboardId = InvalidId;
            }
            else if (_keyboardId > InvalidId)
            {
                if (!InputSystem.Keyboard.HasKeyboardFocus)
                {
                    bool ret;
                    if (_keyboardInputRect.Size != Vector2.Zero)
                        ret = InputSystem.Keyboard.TakeKeyboardFocus(_keyboardInputRect);
                    else
                        ret = InputSystem.Keyboard.TakeKeyboardFocus();

                    if (!ret)
                        _keyboardId = InvalidId;
                }
            }

            _keysPressedSinceLast.Clear();
            _lastInputChar = '\uffff';
        }

        internal void PrintDebug(ImGuiDrawList drawList, ref Vector2 position)
        {
            drawList.DrawText(position, $"Id: Hot:{_hotId} Active:{_activeId} Drag:{_dragId}"); position.Y += ImGuiFont.FontVisualHeight;
            drawList.DrawText(position, $"Keyboard: Id:{_keyboardId} Rect:{_keyboardInputRect} FThisFrame:{_keyboardFocusThisFrame}"); position.Y += ImGuiFont.FontVisualHeight;
            drawList.DrawText(position, $"Window: Active:{_activeWindowId} FocusAppeared:{_hasFocusAppeared}"); position.Y += ImGuiFont.FontVisualHeight;
            drawList.DrawText(position, $"TextState: Buf:{_textState.Buffer.Length} Pos:{_textState.Position} Len:{_textState.Length} SelIdx:{_textState.SelCharIndex} SelCount:{_textState.SelCharCount}"); position.Y += ImGuiFont.FontVisualHeight;
            drawList.DrawText(position, $"Drag:"); position.Y += ImGuiFont.FontVisualHeight;

            position.X += 6.0f;
            for (int i = 0; i < _isDragging.Length; i++)
            {
                drawList.DrawText(position, $"{(MouseButton)i}: Active:{_isDragging[i].IsDragging} Start:{_isDragging[i].StartPos} Delta:{_isDragging[i].Delta}"); position.Y += ImGuiFont.FontVisualHeight;
            }
            position.X -= 6.0f;
        }

        private void GrowIdStack()
        {
            int[] newStack = new int[_idStack.Length * 2];

            Array.Copy(_idStack, newStack, _idStackIndex);
            _idStack = newStack;
        }

        public (bool Hovered, bool Held, bool Pressed) ButtonBehaviour(int id, Boundaries bb, MouseButton button = MouseButton.Left)
        {
            Vector2 mousePos = InputSystem.Pointer.MousePosition;
            if (_hotId == InvalidId && !CheckForOverlap(mousePos) && bb.IsWithin(mousePos))
            {
                _hotId = id;
                if (_activeId == InvalidId && InputSystem.Pointer.IsButtonHeld(button))
                {
                    _activeId = id;
                }
            }

            return (_hotId == id, _activeId == id, InputSystem.Pointer.IsButtonReleased(button) && _hotId == id);
        }

        public (bool HasTakenFocus, bool HasCurrentFocus) TryTakeKeyboardFocus(int id, Boundaries bb)
        {
            if (_keyboardId == InvalidId)
            {
                _keyboardId = id;
                _keyboardInputRect = bb;
                _keyboardFocusThisFrame = true;

                return (true, true);
            }

            if (id == _keyboardId)
            {
                _keyboardFocusThisFrame = true;
                return (false, true);
            }

            return (false, false);
        }

        public bool HasKeyboardFocus(int id) => _keyboardId == id;
        public bool IsWindowActive(int id) => _activeWindowId == id;

        public bool IsLastItemHovered() => _currentWindow != null ? Flags.HasFlag(_currentWindow.LastItemFlags, ImGuiItemStateFlags.Hovered) : false;
        public bool IsLastItemHeld() => _currentWindow != null ? Flags.HasFlag(_currentWindow.LastItemFlags, ImGuiItemStateFlags.Held) : false;

        public bool IsMouseDragging(MouseButton button) => _isDragging[(int)button].IsDragging;
        public Vector2 GetMouseDragDelta(MouseButton button) => _isDragging[(int)button].Delta;

        public (bool IsDragging, Vector2 Position) HandleItemDrag(int id, Vector2 item)
        {
            Vector2 mousePos = InputSystem.Pointer.MousePosition;
            if (_dragId == InvalidId && InputSystem.Pointer.IsButtonHeld(MouseButton.Left))
            {
                _dragId = id;

                _itemDragPosition = item;
                _mouseDragPosition = mousePos;
            }
            else if (!InputSystem.Pointer.IsButtonHeld(MouseButton.Left) && _dragId == id)
            {
                _dragId = InvalidId;
            }

            return (_dragId == id, _itemDragPosition + (mousePos - _mouseDragPosition));
        }

        public void PushId(int id)
        {
            _idStack[_idStackIndex++] = id;
            if (_idStackIndex == _idStack.Length)
                GrowIdStack();
        }

        public void PopId()
        {
            if (_idStackIndex < 1)
                EngLog.ImGui.Error("Cannot pop id on empty stack!");
            else
                --_idStackIndex;
        }

        public int GetId(int top)
        {
            _idStack[_idStackIndex] = top;

            Span<int> data = _idStack.AsSpan(0, _idStackIndex + 1);
            return data.GetDjb2HashCode();
        }

        internal ImGuiWindowState GetOrCreateWindowState(int id, ReadOnlySpan<char> title)
        {
            ImGuiWindowState? windowState = FindWindowById(id);

            if (windowState == null)
            {
                windowState = new ImGuiWindowState
                {
                    Id = id,

                    Title = title.ToString()
                };

                _windowStates.Add(windowState);
            }

            return windowState;
        }

        internal void SetCurrentWindow(ImGuiWindowState? windowState)
        {
            _currentWindow = windowState;

            if (windowState != null && windowState.Id == _activeWindowId)
            {
                _hasFocusAppeared = true;
            }
        }

        public void BringWindowToFront(ReadOnlySpan<char> title)
        {
            ImGuiWindowState? windowState = FindWindowByName(title);
            if (windowState != null)
            {
                windowState.Layer = 1000;
                _windowStates.Sort(static (a, b) => a.Layer.CompareTo(b.Layer));

                int layer = 0;
                for (int i = 0; i < _windowStates.Count; i++)
                {
                    if (Flags.HasFlag(_windowStates[i].WindowFlags, ImGuiWindowFlags.Child))
                        continue;
                    _windowStates[i].Layer = Flags.HasFlag(_windowStates[i].WindowFlags, ImGuiWindowFlags.AlwaysOnTop) ? layer++ + 1000 : layer++;
                }

                _activeWindowId = windowState.Id;
                _hasFocusAppeared = true;
            }
        }

        public ImGuiWindowState? FindWindowById(int id)
        {
            foreach (ImGuiWindowState currentState in _windowStates)
            {
                if (currentState.Id == id)
                {
                    return currentState;
                }
            }

            return null;
        }

        public ImGuiWindowState? FindWindowByName(ReadOnlySpan<char> title)
        {
            foreach (ImGuiWindowState currentState in _windowStates)
            {
                if (currentState.Title.SequenceEqual(title))
                {
                    return currentState;
                }
            }

            return null;
        }

        public Vector2 GetAvailableSpace()
        {
            if (_currentWindow != null)
            {
                return _currentWindow.Size - new Vector2(0.0f, 12.0f) - _context.Style.WindowPadding * new Vector2(2.0f, 3.0f);
            }

            return Vector2.Zero;
        }

        public float CalculateItemWidth()
        {
            return MathF.Min(GetAvailableSpace().X, 100.0f);
        }

        public Vector2 CalculateItemSize(Vector2 targetSize, float width, float height)
        {
            if (targetSize.X == 0.0f)
                targetSize.X = width;
            if (targetSize.Y == 0.0f)
                targetSize.Y = height;

            return targetSize;
        }

        public void BeginHorizontal()
        {
            if (_currentWindow == null || _currentWindow.FlowDirection != ImGuiFlowDirection.Vertical)
                return;

            _currentWindow.FlowDirection = ImGuiFlowDirection.Horizontal;
        }

        public void EndHorizontal()
        {
            if (_currentWindow == null || _currentWindow.FlowDirection != ImGuiFlowDirection.Horizontal)
                return;

            _currentWindow.FlowDirection = ImGuiFlowDirection.Vertical;
        }

        public void AddItemSize(Vector2 size)
        {
            if (_currentWindow == null)
                return;

            float line = _currentWindow.IsSameLine ? _currentWindow.PrevCursorPos.Y : _currentWindow.CursorPos.Y;

            if (_currentWindow.FlowDirection == ImGuiFlowDirection.Vertical)
            {
                float paddingAmount = _currentWindow.IndentLevel > 0 ? _currentWindow.IndentLevel * 8.0f : 0.0f;

                _currentWindow.PrevCursorPos = new Vector2(_currentWindow.CursorPos.X + size.X + _context.Style.InnerItemPadding.X, line);
                _currentWindow.CursorPos = new Vector2(_currentWindow.Position.X + _context.Style.WindowPadding.X + paddingAmount, line + size.Y + _context.Style.ItemPadding.Y);
                _currentWindow.MaxCursorPos = Vector2.Max(_currentWindow.MaxCursorPos, new Vector2(_currentWindow.PrevCursorPos.X, _currentWindow.CursorPos.Y - _context.Style.ItemPadding.Y));
            }
            else if (_currentWindow.FlowDirection == ImGuiFlowDirection.Horizontal)
            {
                _currentWindow.PrevCursorPos = new Vector2(_currentWindow.CursorPos.X + size.X + _context.Style.InnerItemPadding.X, line);
                _currentWindow.CursorPos = _currentWindow.PrevCursorPos;
                _currentWindow.MaxCursorPos = Vector2.Max(_currentWindow.MaxCursorPos, new Vector2(_currentWindow.PrevCursorPos.X, _currentWindow.CursorPos.Y - _context.Style.ItemPadding.Y));
            }

            _currentWindow.IsSameLine = false;
        }

        public void AddItemId(int id, Boundaries bb)
        {
            if (_currentWindow == null)
                return;

            _currentWindow.LastItemId = id;
            _currentWindow.LastItemRect = bb;
            _currentWindow.LastItemFlags = ImGuiItemStateFlags.None;

            Vector2 mousePos = InputSystem.Pointer.MousePosition;

            if (_hotId == id)
                _currentWindow.LastItemFlags |= ImGuiItemStateFlags.Hovered;
            if (_activeId == id)
                _currentWindow.LastItemFlags |= ImGuiItemStateFlags.Held;
        }

        public void SameLine()
        {
            if (_currentWindow == null)
                return;

            _currentWindow.CursorPos = _currentWindow.PrevCursorPos;
            _currentWindow.IsSameLine = true;
        }

        public bool CheckForOverlap(Vector2 point)
        {
            ImGuiWindowState? currWindow = _currentWindow;
            int currLayer = int.MinValue;

            if (currWindow != null)
            {
                if (currWindow.WindowFlags.HasFlags(ImGuiWindowFlags.NoInput))
                    return true;

                if (Flags.HasFlag(currWindow.WindowFlags, ImGuiWindowFlags.Child))
                {
                    do
                    {
                        currWindow = FindWindowById(currWindow!.Parent);
                    } while (currWindow != null && Flags.HasFlag(currWindow.WindowFlags, ImGuiWindowFlags.Child));
                }

                currLayer = currWindow?.Layer ?? int.MinValue;
            }

            for (int i = _windowStates.Count - 1; i >= 0; --i)
            {
                ImGuiWindowState windowState = _windowStates[i];

                if (Flags.HasEither(windowState.WindowFlags, ImGuiWindowFlags.Child | ImGuiWindowFlags.NoInput))
                    continue;
                if (windowState.Layer < currLayer || windowState == currWindow)
                    break;

                if (windowState.Layer > currLayer && new Boundaries(windowState.Position, windowState.Position + windowState.Size).IsWithin(point))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryShowTooltip(int id)
        {
            if (_tooltipWindowId == InvalidId)
            {
                _tooltipWindowId = id;
                return true;
            }

            return false;
        }

        public void Handle(ref readonly SDL_Event @event)
        {
            switch ((SDL_EventType)@event.type)
            {
                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    {
                        _keysPressedSinceLast.Enqueue((@event.key.key, @event.key.mod));
                        break;
                    }
                case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                    {
                        string? str = @event.text.GetText();
                        if (str?.Length > 0)
                            _lastInputChar = str[0];
                        break;
                    }
            }
        }

        public ImGuiWindowState? CurrentWindow => _currentWindow;
        public ImGuiTextState TextState => _textState;

        public Queue<(SDL_Keycode Key, SDL_Keymod Mod)> KeysPressedSinceLast => _keysPressedSinceLast;
        public char LastInputChar => _lastInputChar;

        public const int TransientId = int.MinValue + 1;
        public const int InvalidId = int.MinValue;
    }
}
