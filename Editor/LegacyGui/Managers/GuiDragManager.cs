using Editor.LegacyGui.Elements;
using Hexa.NET.ImGui;
using Primary.Common;
using Primary.Rendering;
using SDL;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Editor.LegacyGui.Managers
{
    public sealed class GuiDragManager
    {
        private Dictionary<Element, PendingDragData> _pendingDrags;

        private Element? _activeDragElement;
        private Action<GuiDragData>? _callback;
        private Vector2 _dragStart;

        internal GuiDragManager()
        {
            _pendingDrags = new Dictionary<Element, PendingDragData>();

            EditorGuiManager.Instance.EventManager.AddViewer(EventViewer);
        }

        private bool EventViewer(ref readonly UIEvent @event)
        {
            if (_activeDragElement != null)
            {
                _pendingDrags.Clear();

                if (@event.Type == UIEventType.MouseMotion)
                {
                    _callback!(new GuiDragData(@event.Hit, @event.Hit - _dragStart, _dragStart, false));
                }
                else if (@event.Type == UIEventType.MouseButtonUp && @event.Mouse.Button == UIMouseButton.Left)
                {
                    _callback!(new GuiDragData(@event.Hit, @event.Hit - _dragStart, _dragStart, true));

                    _activeDragElement = null;
                    _callback = null;

                    if (!SDL.SDL3.SDL_CaptureMouse(false))
                        Log.Error("SDL error: {err}", SDL.SDL3.SDL_GetError());

                    EditorGuiManager.Instance.EventManager.SetExclusiveViewer(null);
                }

                return true;
            }

            if (@event.Type == UIEventType.MouseMotion)
            {
                foreach (var kvp in _pendingDrags)
                {
                    float delta = (kvp.Value.StartLocation - @event.Hit).LengthSquared();
                    if (delta > DragMagnitude)
                    {
                        _activeDragElement = kvp.Key;
                        _callback = kvp.Value.Callback;
                        _dragStart = @event.Hit;

                        if (!SDL.SDL3.SDL_CaptureMouse(true))
                            Log.Error("SDL error: {err}", SDL.SDL3.SDL_GetError());
                        
                        EditorGuiManager.Instance.EventManager.SetExclusiveViewer(EventViewer, kvp.Value.Window);
                    }
                }
            }

            return true;
        }

        public static void TryStartDrag(Element element, Window window, Vector2 startLocation, Action<GuiDragData> dragCallback)
        {
            GuiDragManager dragManager = EditorGuiManager.Instance.DragManager;
            dragManager._pendingDrags[element] = new PendingDragData(startLocation, window, dragCallback);
        }

        public static void CancelDrag(Element element)
        {
            GuiDragManager dragManager = EditorGuiManager.Instance.DragManager;
            if (element == dragManager._activeDragElement)
            {
                dragManager._callback!(new GuiDragData(Vector2.Zero, Vector2.Zero, Vector2.Zero, true));

                dragManager._activeDragElement = null;
                dragManager._callback = null;

                if (!SDL.SDL3.SDL_CaptureMouse(false))
                    Log.Error("SDL error: {err}", SDL.SDL3.SDL_GetError());

                EditorGuiManager.Instance.EventManager.SetExclusiveViewer(null);
            }

            dragManager._pendingDrags.Remove(element);
        }

        public static void ForceStartDrag(Element element, Window window, Vector2 dragStart, Action<GuiDragData> dragCallback)
        {
            GuiDragManager dragManager = EditorGuiManager.Instance.DragManager;

            if (dragManager._activeDragElement == element)
            {
                dragManager._callback = dragCallback;
                return;
            }

            if (dragManager._activeDragElement != null)
                CancelDrag(dragManager._activeDragElement);

            dragManager._pendingDrags.Clear();

            dragManager._activeDragElement = element;
            dragManager._callback = dragCallback;
            dragManager._dragStart = dragStart;

            if (!SDL.SDL3.SDL_CaptureMouse(true))
                Log.Error("SDL error: {err}", SDL.SDL3.SDL_GetError());

            EditorGuiManager.Instance.EventManager.SetExclusiveViewer(dragManager.EventViewer, window);
        }

        private static unsafe Vector2 GrabGlobalMousePosition()
        {
            float x, y;
            SDL.SDL_MouseButtonFlags flags = SDL.SDL3.SDL_GetGlobalMouseState(&x, &y);
            return new Vector2(x, y);
        }

        private static float DragMagnitude = 8.0f;

        private record struct PendingDragData(Vector2 StartLocation, Window Window, Action<GuiDragData> Callback);
    }

    public readonly record struct GuiDragData(Vector2 Position, Vector2 Offset, Vector2 Start, bool IsEnding);
}
