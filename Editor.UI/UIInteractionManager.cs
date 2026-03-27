using Primary;
using Primary.Common;
using Primary.Input.Devices;
using Primary.Mathematics;
using SDL;
using System.Numerics;

namespace Editor.UI
{
    internal sealed class UIInteractionManager : IDisposable
    {
        private readonly UIManager _manager;

        private bool _disposedValue;

        internal UIInteractionManager(UIManager manager)
        {
            _manager = manager;

            Engine.GlobalSingleton.EventManager.EventRecieved += EventCallback;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Engine.GlobalSingleton.EventManager.EventRecieved -= EventCallback;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void EventCallback(SDL_Event @event)
        {
            UIDockHost? dockHost = _manager.FindDockHostFromWindowId((uint)@event.window.windowID);
            if (dockHost == null)
                return;

            switch (@event.Type)
            {
                case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                    {
                        Vector2 position = new Vector2(@event.motion.x, @event.motion.y);
                        Vector2 delta = new Vector2(@event.motion.xrel, @event.motion.yrel);

                        UIDockHost? childHost = FindHostForEvent(dockHost, position);
                        childHost?.InteractionManager?.HandleMouseMotion(position, delta);

                        break;
                    }
                case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                    {
                        Vector2 position = new Vector2(@event.motion.x, @event.motion.y);
                        Vector2 delta = new Vector2(@event.motion.xrel, @event.motion.yrel);

                        UIDockHost? childHost = FindHostForEvent(dockHost, position);
                        childHost?.InteractionManager?.HandleMouseWheel(position, delta);

                        break;
                    }
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                    {
                        Vector2 position = new Vector2(@event.button.x, @event.button.y);
                        MouseButton button = PointerDevice.TranslateButton(@event.button.Button);

                        UIDockHost? childHost = FindHostForEvent(dockHost, position);
                        childHost?.InteractionManager?.HandleMouseDown(position, button);

                        break;
                    }
                case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                    {
                        Vector2 position = new Vector2(@event.button.x, @event.button.y);
                        MouseButton button = PointerDevice.TranslateButton(@event.button.Button);

                        UIDockHost? childHost = FindHostForEvent(dockHost, position);
                        childHost?.InteractionManager?.HandleMouseUp(position, button);

                        break;
                    }
            }
        }

        private static UIDockHost? FindHostForEvent(UIDockHost dockHost, Vector2 position)
        {
            if (Boundaries.Offset(dockHost.TabbedClientBounds, dockHost.ClientOffset.AsVector2()).IsWithin(position))
                return dockHost;

            foreach (UIDockHost dockedHost in dockHost.DockedHosts)
            {
                if (new Boundaries(dockedHost.ClientOffset.AsVector2(), (dockedHost.ClientOffset + dockedHost.ClientSize).AsVector2()).IsWithin(position))
                    return FindHostForEvent(dockedHost, position);
            }

            return null;
        }
    }
}
