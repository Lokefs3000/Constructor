using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using Primary.Rendering;
using SDL;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Primary.GUI.ImGui
{
    public static class IMGUI
    {
        private static ImGuiContext? s_context = null;

        internal static ImGuiContext CreateContext()
        {
            ExceptionUtility.Assert(s_context == null);

            s_context = new ImGuiContext();
            return s_context;
        }

        internal static void DestroyContext()
        {
            ExceptionUtility.Assert(s_context != null);

            s_context.Dispose();
            s_context = null;
        }

        public static void BeginState()
        {
            s_context!.StateController.Prepare();
        }

        public static void EndState()
        {
            if (false)
            {
                ImGuiDrawList drawList = s_context!.GetDrawList();

                drawList.DrawRect(new Vector2(6.0f), new Vector2(600.0f, 120.0f), 0x80ffffff);
                drawList.DrawFilledRect(new Vector2(6.0f + 1.0f), new Vector2(600.0f - 1.0f, 120.0f - 1.0f), 0x80000000);

                Vector2 position = new Vector2(10.0f);
                s_context.StateController.PrintDebug(drawList, ref position);

                s_context.ReturnDrawList(drawList, int.MaxValue);
            }

            s_context!.StateController.Finish();
            s_context.Style.Finish();
        }

        #region Demo
        public static void ShowStyleEditor() => ImGuiStyleEditor.Show();
        #endregion
        #region Utility
        public static Vector2 CalculateTextSize(ReadOnlySpan<char> text)
        {
            Vector2 cursor = Vector2.Zero;
            Vector2 extents = Vector2.Zero;

            for (int i = 0; i < text.Length; ++i)
            {
                char ch = text.DangerousGetReferenceAt(i);
                switch (ch)
                {
                    case ' ':
                        {
                            cursor.X += 4.0f;
                            continue;
                        }
                    case '\n':
                        {
                            extents.X = MathF.Max(extents.X, cursor.X - 1.0f);
                            cursor = new Vector2(0.0f, cursor.Y + ImGuiFont.FontVisualHeight);
                            continue;
                        }
                }

                ImGuiGlyph glyph = s_context!.Font.GetGlyph(ch);

                Vector4 planeBounds = glyph.PlaneBounds + new Vector4(cursor.X, cursor.Y, cursor.X, cursor.Y);
                cursor.X += glyph.Advance;
            }

            extents = Vector2.Max(cursor + new Vector2(-1.0f, ImGuiFont.FontVisualHeight), extents);
            return extents;
        }
        #endregion
        #region State
        public static void PushId(ReadOnlySpan<char> id) => s_context!.StateController.PushId(id.GetDjb2HashCode());
        public static void PushId(int id) => s_context!.StateController.PushId(id);
        public static void PopId() => s_context!.StateController.PopId();
        public static int GetId(ReadOnlySpan<char> id) => s_context!.StateController.GetId(id.GetDjb2HashCode());
        public static int GetId(int id) => s_context!.StateController.GetId(id);

        public static void AddItemId(int id, Boundaries bb) => s_context!.StateController.AddItemId(id, bb);
        public static void AddItemSize(Vector2 size) => s_context!.StateController.AddItemSize(size);

        public static void SameLine() => s_context!.StateController.SameLine();

        public static void Indent()
        {
            ImGuiWindowState? windowState = s_context!.StateController.CurrentWindow;
            if (windowState != null)
            {
                ++windowState.IndentLevel;
                windowState.CursorPos = new Vector2(windowState.CursorPos.X + 8.0f, windowState.CursorPos.Y);
            }
        }
        public static void Unindent()
        {
            ImGuiWindowState? windowState = s_context!.StateController.CurrentWindow;
            if (windowState != null && windowState.IndentLevel > 0)
            {
                --windowState.IndentLevel;
                windowState.CursorPos = new Vector2(windowState.CursorPos.X - 8.0f, windowState.CursorPos.Y);
            }
        }

        public static bool IsLastItemHeld() => s_context!.StateController.IsLastItemHeld();
        public static bool IsLastItemHovered() => s_context!.StateController.IsLastItemHovered();

        public static bool IsMouseDragging(MouseButton button) => s_context!.StateController.IsMouseDragging(button);
        public static Vector2 GetMouseDragDelta(MouseButton button) => s_context!.StateController.GetMouseDragDelta(button);
        #endregion
        #region Behaviour
        public static (bool Hovered, bool Held, bool Pressed) ButtonBehaviour(int id, Boundaries bb, MouseButton button = MouseButton.Left) => s_context!.StateController.ButtonBehaviour(id, bb, button);
        #endregion
        #region Widgets
        public static bool BeginWindow(ReadOnlySpan<char> title, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        {
            ImGuiStyle style = s_context!.Style;

            int windowId = GetId(title);
            if (Flags.HasFlag(flags, ImGuiWindowFlags.Tooltip) && !s_context.StateController.TryShowTooltip(windowId))
                return false;

            PushId(windowId);

            ImGuiWindowState windowState = s_context!.StateController.GetOrCreateWindowState(windowId, title);
            if (windowState.IsNew)
            {
                int parentId = s_context.StateController.CurrentWindow?.Id ?? ImGuiStateController.InvalidId;

                windowState.Parent = parentId;
                windowState.WindowFlags = flags;

                s_context.StateController.BringWindowToFront(title);
            }

            s_context.StateController.SetCurrentWindow(windowState);

            ImGuiDrawList drawList = s_context.GetDrawList();
            windowState.DrawList = drawList;

            bool isTooltip = Flags.HasFlag(flags, ImGuiWindowFlags.Tooltip);

            Vector2 baseline;
            if (isTooltip)
            {
                windowState.Position = InputSystem.Pointer.MousePosition + new Vector2(8.0f, 0.0f);
                baseline = windowState.Position;

                drawList.DrawFilledRect(baseline, baseline + windowState.Size, style.WindowBg.ABGR);
                drawList.DrawRect(baseline - Vector2.One, baseline + windowState.Size + Vector2.One, style.WindowBorder.ABGR);

                windowState.CursorPos = baseline + new Vector2(2.0f);
            }
            else
            {
                int gripId = GetId(2);

                baseline = windowState.Position;

                Boundaries titlebarBb = new Boundaries(windowState.Position, windowState.Position + new Vector2(windowState.Size.X, s_context!.Style.WindowPadding.Y * 2.0f + 7.0f));
                Boundaries gripBb = new Boundaries(baseline + windowState.Size - new Vector2(10.0f), baseline + windowState.Size);

                uint gripCol = 0;
                uint titlebarCol = s_context.StateController.IsWindowActive(windowId) ? style.TitlebarActiveBg.ABGR : style.TitlebarBg.ABGR;

                if (!Flags.HasFlag(flags, ImGuiWindowFlags.NoBackground))
                {
                    var grip = s_context.StateController.ButtonBehaviour(gripId, gripBb);

                    if (grip.Held)
                    {
                        var drag = s_context.StateController.HandleItemDrag(gripId, windowState.Size);
                        if (drag.IsDragging)
                        {
                            windowState.Size = drag.Position;
                        }
                    }

                    gripCol = grip.Held ? 0xb0b8742cu : (grip.Hovered ? 0x90e68c2eu : 0x60c97f30u);
                }

                if (!Flags.HasFlag(flags, ImGuiWindowFlags.NoTitlebar))
                {
                    drawList.DrawFilledRect(titlebarBb.Minimum, titlebarBb.Maximum, titlebarCol);
                    drawList.DrawText(titlebarBb.Minimum + s_context.Style.WindowPadding, title, style.Text.ABGR);
                }

                if (!Flags.HasFlag(flags, ImGuiWindowFlags.NoBackground))
                {
                    drawList.DrawFilledRect(baseline + new Vector2(0.0f, s_context.Style.WindowPadding.Y * 2.0f + 7.0f), baseline + windowState.Size, style.WindowBg.ABGR);
                    drawList.DrawRect(baseline - Vector2.One, baseline + windowState.Size + Vector2.One, style.WindowBorder.ABGR);
                    drawList.DrawFilledTriangle(gripBb.Maximum - new Vector2(10.0f, 0.0f), gripBb.Maximum - new Vector2(0.0f, 10.0f), gripBb.Maximum, gripCol);
                }

                windowState.CursorPos = baseline + new Vector2(s_context.Style.WindowPadding.X, Flags.HasFlag(flags, ImGuiWindowFlags.NoTitlebar) ? s_context.Style.WindowPadding.Y : (s_context.Style.WindowPadding.Y * 3.0f + 8.0f));
                windowState.MaxCursorPos = titlebarBb.Minimum + s_context.Style.WindowPadding + CalculateTextSize(title);
            }

            drawList.PushClip(new Vector4(baseline.X, baseline.Y, baseline.X + windowState.Size.X, baseline.Y + windowState.Size.Y));
            return true;
        }

        public static void EndWindow()
        {
            ImGuiWindowState? windowState = s_context!.StateController.CurrentWindow;
            if (windowState != null)
            {
                if (Flags.HasFlag(windowState.WindowFlags, ImGuiWindowFlags.Child))
                    return;

                bool alwaysResize = Flags.HasFlag(windowState.WindowFlags, ImGuiWindowFlags.AlwaysResize);
                if (windowState.IsNew || alwaysResize)
                {
                    Vector2 newSize = Vector2.Max(windowState.MaxCursorPos - windowState.Position + s_context.Style.WindowPadding * 6.0f, new Vector2(40.0f));
                    windowState.Size = newSize;
                    windowState.IsNew = false;
                }

                windowState.DrawList.PopClip();

                if (!Flags.HasEither(windowState.WindowFlags, ImGuiWindowFlags.NoMove | (ImGuiWindowFlags)(1 << 0)))
                {
                    int bodyId = GetId(1);
                    Boundaries bodyBb = new Boundaries(windowState.Position, windowState.Position + windowState.Size);

                    var body = s_context.StateController.ButtonBehaviour(bodyId, bodyBb);

                    if (body.Held)
                    {
                        var drag = s_context.StateController.HandleItemDrag(bodyId, windowState.Position);
                        if (drag.IsDragging)
                        {
                            windowState.Position = drag.Position;
                        }

                        s_context.StateController.BringWindowToFront(windowState.Title);
                    }
                }

                s_context.ReturnDrawList(windowState.DrawList, windowState.Layer);

                if (windowState.Parent != ImGuiStateController.InvalidId)
                    s_context.StateController.SetCurrentWindow(s_context.StateController.FindWindowById(windowState.Parent));
                else
                    s_context.StateController.SetCurrentWindow(null);

                windowState.ResetState();
                PopId();
            }
        }

        public static bool BeginTooltip()
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;
            if (window == null)
                return false;

            if (!Flags.HasFlag(window.LastItemFlags, ImGuiItemStateFlags.Hovered))
                return false;

            return BeginWindow("Tooltip", ImGuiWindowFlags.Tooltip);
        }

        public static void EndTooltip() => EndWindow();

        public static bool BeginChild(ReadOnlySpan<char> childId, ImGuiChildFlags childFlags = ImGuiChildFlags.None) => BeginChild(childId.GetDjb2HashCode(), Vector2.Zero, childFlags);
        public static bool BeginChild(int childId, ImGuiChildFlags childFlags = ImGuiChildFlags.None) => BeginChild(childId, Vector2.Zero, childFlags);

        public static bool BeginChild(ReadOnlySpan<char> childId, Vector2 size, ImGuiChildFlags childFlags = ImGuiChildFlags.None) => BeginChild(childId.GetDjb2HashCode(), size, childFlags);
        public static bool BeginChild(int childId, Vector2 size, ImGuiChildFlags childFlags = ImGuiChildFlags.None)
        {
            int id = GetId(childId);

            ImGuiWindowState? parentState = s_context!.StateController.CurrentWindow;
            if (parentState == null)
                return false;

            ImGuiWindowState windowState = s_context!.StateController.GetOrCreateWindowState(id, ReadOnlySpan<char>.Empty);
            if (windowState.IsNew)
            {
                windowState.Parent = parentState.Id;
                windowState.WindowFlags = ImGuiWindowFlags.Child;

                if (Flags.HasFlag(childFlags, ImGuiChildFlags.AlwaysResize))
                    windowState.WindowFlags |= ImGuiWindowFlags.AlwaysResize;
            }

            windowState.Position = parentState.CursorPos;
            windowState.CursorPos = parentState.CursorPos + s_context!.Style.WindowPadding;

            if (size.X == 0.0f)
                size.X = windowState.Size.X;
            if (size.Y == 0.0f)
                size.Y = windowState.Size.Y;

            s_context!.StateController.AddItemSize(size);
            s_context!.StateController.SetCurrentWindow(windowState);

            ImGuiDrawList drawList = parentState.DrawList!;

            windowState.DrawList = parentState.DrawList;

            Vector2 maxExtents = windowState.CursorPos + size - s_context!.Style.WindowPadding * 2.0f;

            PushId(childId);

            if (Flags.HasFlag(childFlags, ImGuiChildFlags.Borders))
                drawList.DrawRect(windowState.Position, windowState.Position + size, s_context.Style.ChildBorder.ABGR);
            drawList.PushClip(new Vector4(windowState.CursorPos.X, windowState.CursorPos.Y, maxExtents.X, maxExtents.Y), true);

            return true;
        }

        public static void EndChild()
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window != null)
            {
                if (!Flags.HasFlag(window.WindowFlags, ImGuiWindowFlags.Child))
                    return;

                bool alwaysResize = Flags.HasFlag(window.WindowFlags, ImGuiWindowFlags.AlwaysResize);
                if (window.IsNew || alwaysResize)
                {
                    Vector2 newSize = Vector2.Max(window.MaxCursorPos - window.Position + s_context.Style.WindowPadding * 2.0f, new Vector2(40.0f));
                    window.Size = newSize;
                    window.IsNew = false;
                }

                if (window.Parent != ImGuiStateController.InvalidId)
                    s_context.StateController.SetCurrentWindow(s_context.StateController.FindWindowById(window.Parent));
                else
                    s_context.StateController.SetCurrentWindow(null);
            }

            window?.DrawList.PopClip();
            window?.ResetState();
        }

        public static bool BeginMenuBar()
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window == null)
                return false;

            ImGuiStyle style = s_context.Style;

            Vector2 min = new Vector2(window.Position.X, window.Position.Y + s_context!.Style.WindowPadding.Y * 2.0f + 7.0f);
            window.CursorPos = min + s_context!.Style.FramePadding;

            window.DrawList.DrawFilledRect(min, min + new Vector2(window.Size.X, 7.0f + s_context!.Style.FramePadding.Y * 2.0f), style.MenubarBg.ABGR);
            window.DrawList.PushClip(new Vector4(min.X, min.Y, window.Size.X - s_context!.Style.FramePadding.X * 2.0f, min.Y + 7.0f + s_context!.Style.FramePadding.Y * 2.0f));

            s_context!.StateController.BeginHorizontal();

            return true;
        }
        public static void EndMenuBar()
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window == null)
                return;

            window.DrawList.PopClip();

            s_context!.StateController.EndHorizontal();
            s_context.StateController.AddItemSize(new Vector2(window.Size.X, s_context!.Style.FramePadding.Y * 2.0f + 7.0f));
        }

        public static void Text(ReadOnlySpan<char> text)
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window == null)
                return;

            window.DrawList.DrawText(window.CursorPos, text, s_context.Style.Text.ABGR);
            s_context!.StateController.AddItemSize(CalculateTextSize(text));
        }

        public static void TextColored(ReadOnlySpan<char> text, uint color = 0xffffffff)
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window == null)
                return;

            window.DrawList.DrawText(window.CursorPos, text, color);
            s_context!.StateController.AddItemSize(CalculateTextSize(text));
        }

        public static bool Button(ReadOnlySpan<char> text, bool isSelected = false) => Button(text, Vector2.Zero, isSelected);
        public static bool Button(ReadOnlySpan<char> text, Vector2 size, bool isSelected = false)
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window == null)
                return false;

            ImGuiStyle style = s_context.Style;

            Vector2 textSize = CalculateTextSize(text);
            int id = GetId(text);

            if (size.X == 0)
                size.X = textSize.X + 5.0f;
            if (size.Y == 0)
                size.Y = textSize.Y + 4.0f;

            Boundaries bb = new Boundaries(window.CursorPos, window.CursorPos + size);
            var state = s_context!.StateController.ButtonBehaviour(id, bb);

            uint color = style.FrameBg.ABGR;
            if (state.Held)
                color = style.FrameActiveBg.ABGR;
            else if (state.Hovered)
                color = style.FrameHoveredBg.ABGR;

            window.DrawList.DrawFilledRect(bb.Minimum, bb.Maximum, color);
            window.DrawList.DrawText(bb.Minimum + new Vector2(2.0f, 2.0f), text, style.Text.ABGR);

            if (isSelected)
            {
                window.DrawList.DrawRect(bb.Minimum + new Vector2(0.5f), bb.Maximum - new Vector2(0.5f), 0xfff5922a);
            }

            s_context!.StateController.AddItemSize(size);
            s_context!.StateController.AddItemId(id, bb);

            return state.Pressed;
        }

        public static bool InputString(ReadOnlySpan<char> text, ref string v, int maxLength = int.MaxValue)
        {
            ReadOnlySpan<char> span = v.AsSpan();
            if (InputString(text, ref span, Vector2.Zero, maxLength))
            {
                v = span.ToString();
                return true;
            }

            return false;
        }

        public static bool InputString(ReadOnlySpan<char> text, ref string v, Vector2 size, int maxLength = int.MaxValue)
        {
            ReadOnlySpan<char> span = v.AsSpan();
            if (InputString(text, ref span, size, maxLength))
            {
                v = span.ToString();
                return true;
            }

            return false;
        }


        public static bool InputString(ReadOnlySpan<char> text, ref ReadOnlySpan<char> v, int maxLength = int.MaxValue) => InputString(text, ref v, Vector2.Zero, maxLength);
        public static bool InputString(ReadOnlySpan<char> text, ref ReadOnlySpan<char> v, Vector2 size, int maxLength = int.MaxValue)
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window == null)
                return false;

            ImGuiStyle style = s_context.Style;

            int id = GetId(text);
            bool changed = false;

            Vector2 labelSize = CalculateTextSize(text);

            Vector2 frameSize = s_context!.StateController.CalculateItemSize(size, s_context!.StateController.CalculateItemWidth(), labelSize.Y + s_context!.Style.FramePadding.Y * 2.0f);
            float frameOffset = (labelSize.X > 0.0f ? s_context!.Style.InnerItemPadding.X + labelSize.X : 0.0f);

            Vector2 fullSize = new Vector2(frameSize.X + (labelSize.X > 0.0f ? s_context!.Style.InnerItemPadding.X + labelSize.X : 0.0f), frameSize.Y);

            Boundaries fullBb = new Boundaries(window.CursorPos, window.CursorPos + fullSize);
            Boundaries frameBb = new Boundaries(window.CursorPos + new Vector2(frameOffset, 0.0f), window.CursorPos + new Vector2(frameOffset, 0.0f) + frameSize);

            var state = s_context!.StateController.ButtonBehaviour(id, frameBb);
            bool hasKeyboardFocus = s_context!.StateController.HasKeyboardFocus(id);

            uint frameColor = style.FrameBg.ABGR;
            if (!hasKeyboardFocus)
            {
                if (state.Held)
                    frameColor = style.FrameActiveBg.ABGR;
                else if (state.Hovered)
                    frameColor = style.FrameHoveredBg.ABGR;
            }

            if (state.Pressed || hasKeyboardFocus)
            {
                if (!(hasKeyboardFocus && InputSystem.Pointer.IsButtonReleased(MouseButton.Left) && !state.Hovered))
                {
                    (bool hasTakenFocus, hasKeyboardFocus) = s_context!.StateController.TryTakeKeyboardFocus(id, frameBb);
                    if (hasKeyboardFocus)
                    {
                        ImGuiTextState textState = s_context!.StateController.TextState;
                        if (hasTakenFocus)
                        {
                            textState.ResetState();
                            if (maxLength <= 1024)
                                textState.ResizeToAtleast(maxLength);
                            textState.Append(v);
                        }

                        while (s_context!.StateController.KeysPressedSinceLast.TryDequeue(out var kc))
                        {
                            if (kc.Key == SDL_Keycode.SDLK_BACKSPACE)
                            {
                                if (textState.SelCharIndex > 0 || textState.SelCharCount > 0)
                                {
                                    if (textState.SelCharCount > 0)
                                    {
                                        textState.RemoveRange(textState.SelCharIndex--, textState.SelCharCount);
                                        textState.SelCharCount = 0;
                                        changed = true;
                                    }
                                    else
                                    {
                                        textState.RemoveAt(textState.SelCharIndex--);
                                        changed = true;
                                    }
                                }
                            }
                            else if (kc.Key == SDL_Keycode.SDLK_RIGHT)
                            {
                                if (kc.Mod.HasFlag(SDL_Keymod.SDL_KMOD_LSHIFT))
                                {
                                    if (textState.SelCharIndex + textState.SelCharCount < textState.Length)
                                    {
                                        textState.SelCharCount++;
                                    }
                                }
                                else
                                {
                                    if (textState.SelCharCount > 0)
                                    {
                                        textState.SelCharIndex += textState.SelCharCount;
                                        textState.SelCharCount = 0;
                                    }
                                    else if (textState.SelCharIndex < textState.Length)
                                    {
                                        textState.SelCharIndex++;
                                    }
                                }
                            }
                            else if (kc.Key == SDL_Keycode.SDLK_LEFT)
                            {
                                if (kc.Mod.HasFlag(SDL_Keymod.SDL_KMOD_LSHIFT))
                                {
                                    if (textState.SelCharIndex > 0)
                                    {
                                        textState.SelCharIndex--;
                                        textState.SelCharCount++;
                                    }
                                }
                                else
                                {
                                    if (textState.SelCharCount > 0)
                                    {
                                        textState.SelCharCount = 0;
                                    }
                                    else if (textState.SelCharIndex > 0)
                                    {
                                        textState.SelCharIndex--;
                                    }
                                }
                            }
                        }

                        textState.SelCharIndex = Math.Clamp(textState.SelCharIndex, 0, textState.Length);
                        textState.SelCharCount = Math.Clamp(textState.SelCharCount, 0, textState.Length - textState.SelCharIndex);

                        if (textState.Length < maxLength && s_context!.StateController.LastInputChar != '\uffff')
                        {
                            textState.Insert(textState.SelCharIndex++, s_context!.StateController.LastInputChar);
                            changed = true;
                        }

                        if (changed)
                        {
                            v = textState.AsSpan();
                        }
                    }
                }
            }

            window.DrawList.DrawText(fullBb.Minimum, text, 0xffffffff);
            window.DrawList.DrawFilledRect(frameBb.Minimum, frameBb.Maximum, frameColor);

            if (hasKeyboardFocus)
            {
                ReadOnlySpan<char> writer = s_context!.StateController.TextState.AsSpan();

                window.DrawList.DrawRect(frameBb.Minimum + Vector2.One, frameBb.Maximum, 0xfff5922a);
                window.DrawList.DrawText(frameBb.Minimum + s_context!.Style.FramePadding, writer, 0xffffffff);

                ImGuiTextState textState = s_context!.StateController.TextState;
                if (textState.SelCharCount > 0)
                {
                    Vector2 highlightMin = frameBb.Minimum + s_context!.Style.FramePadding + new Vector2(CalculateTextSize(writer.Slice(0, textState.SelCharIndex)).X, 0.0f);
                    window.DrawList.DrawFilledRect(highlightMin, highlightMin + CalculateTextSize(writer.Slice(textState.SelCharIndex, textState.SelCharCount)), 0x800000ff);
                }

                Vector2 lineMin = frameBb.Minimum + s_context!.Style.FramePadding + new Vector2(1.0f, labelSize.Y + 1.0f);
                if (textState.SelCharIndex + textState.SelCharCount > 0)
                {
                    lineMin.X += CalculateTextSize(writer.Slice(0, textState.SelCharIndex + textState.SelCharCount)).X;
                }
                window.DrawList.DrawLine(lineMin, lineMin + new Vector2(5.0f, 0.0f), (uint)(Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 2) % 2 == 0 ? 0xffffffffu : 0xb0ffffffu));
            }
            else
            {
                window.DrawList.DrawText(frameBb.Minimum + s_context!.Style.FramePadding, v, 0xffffffff);
            }

            s_context!.StateController.AddItemSize(fullSize);
            s_context!.StateController.AddItemId(id, frameBb);

            return changed;
        }

        public static bool InputInt(ReadOnlySpan<char> text, ref int v, int min = int.MinValue, int max = int.MaxValue)
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window == null)
                return false;

            int id = GetId(text);
            bool changed = false;

            PushId(id);

            Span<char> formatted = stackalloc char[10];
            if (!v.TryFormat(formatted, out int charsWritten))
            {
                PopId();
                return false;
            }

            formatted = formatted.Slice(0, charsWritten);

            ReadOnlySpan<char> temp = formatted;
            if (InputString(text, ref temp))
            {
                if (int.TryParse(temp, out int result))
                {
                    int newVal = Math.Clamp(result, min, max);

                    if (newVal != v)
                    {
                        v = newVal;
                        changed = true;
                    }
                }
            }

            SameLine();

            if (Button("-"))
            {
                v = Math.Clamp(v - 1, min, max);
                changed = true;
            }
            s_context!.StateController.SameLine();
            if (Button("+"))
            {
                v = Math.Clamp(v + 1, min, max);
                changed = true;
            }

            PopId();

            return changed;
        }

        public static bool InputVector2(ReadOnlySpan<char> text, ref Vector2 v)
        {
            return InputScalarVector(text, MemoryMarshal.CreateSpan(ref v.X, 2));
        }

        public static bool DragScalar<T>(ReadOnlySpan<char> text, ref T value) where T : unmanaged, IFormattable, INumber<T> => DragScalar(text, ref value, Vector2.Zero);
        public static bool DragScalar<T>(ReadOnlySpan<char> text, ref T value, Vector2 size) where T : unmanaged, IFormattable, INumber<T>
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window == null)
                return false;

            ImGuiStyle style = s_context.Style;

            int id = GetId(text);
            bool changed = false;

            Vector2 labelSize = CalculateTextSize(text);

            Vector2 frameSize = s_context!.StateController.CalculateItemSize(size, s_context!.StateController.CalculateItemWidth(), labelSize.Y + s_context!.Style.FramePadding.Y * 2.0f);
            float frameOffset = (labelSize.X > 0.0f ? s_context!.Style.InnerItemPadding.X + labelSize.X : 0.0f);

            Vector2 fullSize = new Vector2(frameSize.X + (labelSize.X > 0.0f ? s_context!.Style.InnerItemPadding.X + labelSize.X : 0.0f), frameSize.Y);

            Boundaries fullBb = new Boundaries(window.CursorPos, window.CursorPos + fullSize);
            Boundaries frameBb = new Boundaries(window.CursorPos + new Vector2(frameOffset, 0.0f), window.CursorPos + new Vector2(frameOffset, 0.0f) + frameSize);
            
            window.DrawList.DrawText(fullBb.Minimum, text, 0xffffffff);
            
            Span<char> formatted = stackalloc char[16];
            if (!value.TryFormat(formatted, out int charsWritten, "G", CultureInfo.InvariantCulture))
            {
                PopId();
                return false;
            }

            Vector2 valueSize = CalculateTextSize(formatted[..charsWritten]);

            var state = s_context!.StateController.ButtonBehaviour(id, frameBb);

            uint frameColor = style.FrameBg.ABGR;
            if (state.Held)
                frameColor = style.FrameActiveBg.ABGR;
            else if (state.Hovered)
                frameColor = style.FrameHoveredBg.ABGR;

            var dragState = s_context.StateController.HandleItemDrag(id, new Vector2(0.0f, 0.0f));
            if (dragState.IsDragging)
            {
                float delta = dragState.Position.X;
            }

            window.DrawList.DrawFilledRect(frameBb.Minimum, frameBb.Maximum, frameColor);
            window.DrawList.DrawText(frameBb.Center - valueSize * 0.5f, formatted[..charsWritten]);

            s_context.StateController.AddItemSize(fullSize);
            s_context.StateController.AddItemId(id, frameBb);

            return changed;
        }

        public static bool DragVector2(ReadOnlySpan<char> text, ref Vector2 v)
        {
            return DragScalarVector(text, MemoryMarshal.CreateSpan(ref v.X, 2));
        }

        public static (bool IsNodeOpen, bool IsLabelPressed) TreeNode(ReadOnlySpan<char> text, TreeNodeFlags flags = TreeNodeFlags.None)
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window == null)
                return (false, false);

            ImGuiStyle style = s_context.Style;

            PushId(text);

            int arrowId = GetId(0);
            int labelId = GetId(1);

            Boundaries arrowBb = new Boundaries(window.CursorPos, window.CursorPos + new Vector2(ImGuiFont.FontVisualHeight));
            Boundaries labelBb = new Boundaries(new Vector2(window.CursorPos.X + ImGuiFont.FontVisualHeight + style.InnerItemPadding.X, window.CursorPos.Y), new Vector2(AvailableSize.X, arrowBb.Maximum.Y));

            var arrowState = ButtonBehaviour(arrowId, arrowBb);
            var labelState = ButtonBehaviour(labelId, labelBb);

            if (arrowState.Held || arrowState.Hovered)
                window.DrawList.DrawFilledRect(arrowBb.Minimum, arrowBb.Maximum, arrowState.Held ? style.FrameActiveBg.ABGR : style.FrameHoveredBg.ABGR);
            
            if (labelState.Held || labelState.Hovered)
                window.DrawList.DrawFilledRect(labelBb.Minimum, labelBb.Maximum, labelState.Held ? style.FrameActiveBg.ABGR : style.FrameHoveredBg.ABGR);

            window.DrawList.DrawFilledTriangle(arrowBb.Minimum, new Vector2(float.Lerp(arrowBb.Minimum.X, arrowBb.Maximum.X, 0.5f), arrowBb.Maximum.Y), new Vector2(arrowBb.Maximum.X, arrowBb.Minimum.Y));
            window.DrawList.DrawText(labelBb.Minimum, text, style.Text.ABGR);

            ++window.TreeDepth;
            Indent();

            return (true, labelState.Pressed);
        }

        public static void TreePop()
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window == null)
                return;

            if (window.TreeDepth > 0)
                --window.TreeDepth;
            else
                EngLog.ImGui.Error("Cannot pop the tree stack because it is empty");

            Unindent();
        }

        #region Internal
        private static bool InputScalarVector<T>(ReadOnlySpan<char> text, Span<T> values) where T : unmanaged, IFormattable, INumber<T>
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window == null)
                return false;

            ImGuiStyle style = s_context.Style;

            int id = GetId(text);
            bool changed = false;

            PushId(id);

            Vector2 labelSize = CalculateTextSize(text);

            window.DrawList.DrawText(window.CursorPos, text, 0xffffffff);
            window.CursorPos += new Vector2(labelSize.X + style.InnerItemPadding.X, 0.0f);

            Span<char> formatted = stackalloc char[10];

            int elementCount = values.Length;
            for (int i = 0; i < elementCount; ++i)
            {
                PushId(i);

                if (!values[i].TryFormat(formatted, out int charsWritten, "G", CultureInfo.InvariantCulture))
                {
                    PopId();
                    continue;
                }

                ReadOnlySpan<char> temp = formatted[..charsWritten];
                if (InputString([], ref temp))
                {
                    if (T.TryParse(temp, CultureInfo.InvariantCulture, out T result))
                    {
                        if (result != values[i])
                        {
                            values[i] = result;
                            changed = true;
                        }
                    }
                }

                if (i < elementCount - 1)
                {
                    SameLine();
                }

                PopId();
            }

            PopId();

            return changed;
        }

        private static bool DragScalarVector<T>(ReadOnlySpan<char> text, Span<T> values) where T : unmanaged, IFormattable, INumber<T>
        {
            ImGuiWindowState? window = s_context!.StateController.CurrentWindow;

            if (window == null)
                return false;

            ImGuiStyle style = s_context.Style;

            int id = GetId(text);
            bool changed = false;

            PushId(id);

            Vector2 labelSize = CalculateTextSize(text);

            window.DrawList.DrawText(window.CursorPos, text, 0xffffffff);
            window.CursorPos += new Vector2(labelSize.X + style.InnerItemPadding.X, 0.0f);

            int elementCount = values.Length;
            for (int i = 0; i < elementCount; ++i)
            {
                PushId(i);

                if (DragScalar([], ref values[i]))
                {
                    changed = true;
                }

                if (i < elementCount - 1)
                {
                    SameLine();
                }

                PopId();
            }

            PopId();

            return changed;
        }
        #endregion
        #endregion
        #region Getters
        public static ImGuiWindowState? CurrentWindow => s_context!.StateController.CurrentWindow;

        public static ImGuiStyle Style => s_context!.Style;

        public static Vector2 CursorPosition => s_context!.StateController.CurrentWindow?.CursorPos ?? Vector2.Zero;
        public static Vector2 AvailableSize => s_context!.StateController.GetAvailableSpace();
        #endregion

        public static ImGuiContext? CurrentContext => s_context;
    }

    public enum TreeNodeFlags : byte
    {
        None = 0,

        Leaf = 1 << 0
    }
}
