using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Backends.SDL3;
using Hexa.NET.OpenGL;
using Hexa.NET.SDL3;
using HexaGen.Runtime;
using SDLWindow = Hexa.NET.SDL3.SDLWindow;

namespace ExtConsole
{
    internal unsafe sealed class BackendManager : IDisposable
    {
        private SDLWindow* _window;
        private SDLGLContext _context;

        private GL _gl;

        private bool _disposedValue;

        internal BackendManager()
        {
            _window = SDL.CreateWindow("ExtConsole", 800, 500, (ulong)(SDLWindowFlags.Resizable | SDLWindowFlags.Opengl));
            _context = SDL.GLCreateContext(_window);

            _gl = new GL(new GLContext((nint)_window, _context));

            ImGuiContextPtr context = ImGui.CreateContext();
            ImGuiImplOpenGL3.SetCurrentContext(context);
            ImGuiImplSDL3.SetCurrentContext(context);

            ImGuiImplOpenGL3.Init("#version 330");
            ImGuiImplSDL3.InitForOpenGL((Hexa.NET.ImGui.Backends.SDL3.SDLWindow*)_window, _context.Handle.ToPointer());

            ImGuiIOPtr io = ImGui.GetIO();

            {
                ImGuiStylePtr style = ImGui.GetStyle();
                style.Colors[(int)ImGuiCol.MenuBarBg] = style.Colors[(int)ImGuiCol.WindowBg];
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                ImGuiImplSDL3.Shutdown();
                ImGuiImplOpenGL3.Shutdown();
                ImGui.DestroyContext();

                _gl.Dispose();

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

        public void ClearWindow()
        {
            _gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            _gl.Clear(GLClearBufferMask.ColorBufferBit);
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

        private record class GLContext(nint Window, SDLGLContext Context) : IGLContext
        {
            public void Dispose() { }

            public nint GetProcAddress(string procName) => (nint)SDL.GLGetProcAddress(procName);

            public bool IsExtensionSupported(string extensionName) => SDL.GLExtensionSupported(extensionName);

            public void MakeCurrent() => SDL.GLMakeCurrent((SDLWindow*)Window, Context);

            public void SwapBuffers() => SDL.GLSwapWindow((SDLWindow*)Window);

            public void SwapInterval(int interval) => SDL.GLSetSwapInterval(interval);

            public bool TryGetProcAddress(string procName, out nint procAddress)
            {
                procAddress = (nint)SDL.GLGetProcAddress(procName);
                return procAddress != nint.Zero;
            }

            public nint Handle => Context.Handle;
            public bool IsCurrent => SDL.GLGetCurrentContext() == Context;
        }
    }
}
