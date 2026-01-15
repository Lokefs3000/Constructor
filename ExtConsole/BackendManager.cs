using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Backends.SDL3;
using Hexa.NET.SDL3;
using System.Runtime.InteropServices;
using SDLWindow = Hexa.NET.SDL3.SDLWindow;

namespace ExtConsole
{
    internal unsafe sealed class BackendManager : IDisposable
    {
        private SDLWindow* _window;
        private SDLGLContext _context;

        private bool _disposedValue;

        internal BackendManager()
        {
            _window = SDL.CreateWindow("ExtConsole", 800, 500, SDLWindowFlags.Resizable | SDLWindowFlags.Opengl);
            _context = SDL.GLCreateContext(_window);

            ImGuiContextPtr context = ImGui.CreateContext();
            ImGuiImplOpenGL3.SetCurrentContext(context);
            ImGuiImplSDL3.SetCurrentContext(context);

            ImGuiImplOpenGL3.Init("#version 330");
            ImGuiImplSDL3.InitForOpenGL((Hexa.NET.ImGui.Backends.SDL3.SDLWindow*)_window, _context.Handle.ToPointer());

            ImGuiIOPtr io = ImGui.GetIO();
            
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                ImGuiImplSDL3.Shutdown();
                ImGuiImplOpenGL3.Shutdown();
                ImGui.DestroyContext();

                SDL.GLDestroyContext(_context);
                SDL.DestroyWindow(_window);

                _disposedValue = true;
            }
        }

        ~BackendManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void PresentWindow()
        {
            SDL.GLSwapWindow(_window);
        }

        public void GetWindowSize(out int width, out int height)
        {
            width = 0;
            height = 0;

            SDL.GetWindowSizeInPixels(_window, ref width, ref height);
        }
    }
}
