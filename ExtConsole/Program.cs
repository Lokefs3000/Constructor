using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.SDL3;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ExtConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using BackendManager backend = new BackendManager();

            bool isRunning = true;
            while (isRunning)
            {
                {
                    SDLEvent @event = new SDLEvent();
                    while (SDL.PollEvent(ref @event))
                    {
                        if (@event.Type == (uint)SDLEventType.WindowCloseRequested)
                        {
                            isRunning = false;
                            break;
                        }

                        Hexa.NET.ImGui.Backends.SDL3.ImGuiImplSDL3.ProcessEvent(
                            ref Unsafe.As<SDLEvent, Hexa.NET.ImGui.Backends.SDL3.SDLEvent>(ref @event));
                    }
                }

                Hexa.NET.ImGui.Backends.SDL3.ImGuiImplSDL3.NewFrame();
                ImGuiImplOpenGL3.NewFrame();
                ImGui.NewFrame();

                backend.GetWindowSize(out int width, out int height);
                


                ImGui.Render();
                ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

                backend.PresentWindow();
            }
        }
    }
}