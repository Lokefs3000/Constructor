using ExtConsole.Communication;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.SDL3;
using Primary.Common;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ExtConsole
{
    public sealed class Program
    {
        public static void Main(string[] args)
        {
            AppArguments.Parse(args);

            int customPort = 14000;
            {
                int i = 0;
                if ((i = args.IndexOf("--host-port")) != -1)
                {
                    customPort = int.Parse(args[i + 1]);
                }
            }

            using ExtConsoleHost host = new ExtConsoleHost(customPort);
            using BackendManager backend = new BackendManager();
            /*using*/ WindowManager windows = new WindowManager(host);

            host.TryStart(true);

            bool isRunning = true;
            if (AppArguments.HasArgument("--attached"))
            {
                host.ClientDisconnected += (x) => isRunning = false;
            }

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

                host.PumpEvents();

                Hexa.NET.ImGui.Backends.SDL3.ImGuiImplSDL3.NewFrame();
                ImGuiImplOpenGL3.NewFrame();
                ImGui.NewFrame();

                backend.GetWindowSize(out int width, out int height);
                windows.Render(new Vector2(width, height), host);

                ImGui.Render();

                backend.ClearWindow();
                ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());
                backend.PresentWindow();

                PreciseWait.Wait(1.0 / 60.0);
            }

            host.TryClose();
        }
    }
}