using Primary.Common;
using SDL;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static SDL.SDL3;

namespace Primary.Polling
{
    public unsafe class EventManager : IDisposable
    {
        private static EventManager? s_instance = null;

        private List<IEventHandler> _handlers;

        private bool _disposedValue;

        internal EventManager()
        {
            s_instance = this;

            _handlers = new List<IEventHandler>();

            //ExceptionUtility.Assert(SDL_AddEventWatch(&EventWatchCallback, nint.Zero));
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static SDLBool EventWatchCallback(nint userData, SDL_Event* data)
        {
            ref SDL_Event @event = ref Unsafe.AsRef<SDL_Event>(data);
            EventManager manager = s_instance!;

            if (@event.type == (uint)SDL_EventType.SDL_EVENT_WINDOW_MOVED || @event.type == (uint)SDL_EventType.SDL_EVENT_WINDOW_RESIZED)
            {
                manager.PumpOnRecieve(ref @event);
                manager.PumpDefaultPause?.Invoke();
            }

            return true;
        }

        public void PollEvents()
        {
            unsafe
            {
                SDL_Event @event = new SDL_Event();
                while (SDL_PollEvent(&@event))
                {
                    PumpOnRecieve(ref @event);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PumpOnRecieve(ref SDL_Event @event)
        {
            foreach (IEventHandler handler in _handlers)
            {
                handler.Handle(ref @event);
            }

            EventRecieved?.Invoke(@event);
        }

        public void AddHandler<T>(T handler) where T : IEventHandler
        {
            if (!_handlers.Contains(handler))
                _handlers.Add(handler);
        }

        public void RemoveHandler<T>(T handler) where T : IEventHandler
        {
            _handlers.Remove(handler);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    s_instance = null;
                }

                //SDL_RemoveEventWatch(&EventWatchCallback, nint.Zero);

                _disposedValue = true;
            }
        }

        ~EventManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public event Action? PumpDefaultPause = null;
        public event Action<SDL_Event>? EventRecieved = null;
    }

    public interface IEventHandler
    {
        public void Handle(ref readonly SDL_Event @event);
    }
}
