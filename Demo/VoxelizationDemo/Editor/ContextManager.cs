using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.SDL3;
using Primary.Polling;
using Primary.Windowing;
using SDL;

namespace VoxelizationDemo.Editor
{
    internal sealed class ContextManager : IDisposable, IEventHandler
    {
        private readonly ImGuiContextPtr _context;
        private bool _isSDL3Setup;

        private bool _disposedValue;

        internal ContextManager()
        {
            _context = ImGui.CreateContext();
            _isSDL3Setup = false;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_isSDL3Setup)
                        ImGuiImplSDL3.Shutdown();
                    ImGui.DestroyContext();
                }

                _disposedValue = true;
            }
        }

        ~ContextManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void Handle(ref readonly SDL_Event @event)
        {
            if (_isSDL3Setup)
            {
                SDL_Event eventCopy = @event;
                ImGuiImplSDL3.ProcessEvent(ref Unsafe.As<SDL_Event, SDLEvent>(ref eventCopy));
            }
        }

        internal void StartFrame()
        {
            if (!_isSDL3Setup)
            {
                unsafe
                {
                    ImGuiImplSDL3.SetCurrentContext(_context);
                    ImGuiImplSDL3.InitForOther(new SDLWindowPtr((SDLWindow*)WindowManager.Instance.PrimaryWindow!.InternalWindowInterop));
                }

                _isSDL3Setup = true;
            }

            ImGuiImplSDL3.NewFrame();
            ImGui.NewFrame();
        }

        internal void EndFrame()
        {
            ImGui.Render();
        }

        internal ImGuiContextPtr ImGuiCtx => _context;
    }
}
