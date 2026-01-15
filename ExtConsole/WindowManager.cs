using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ExtConsole
{
    internal sealed class WindowManager
    {
        private IWindow[] _windows;

        internal WindowManager()
        {
            _windows = [

                ];
        }

        public void Render(Vector2 windowSize)
        {
            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(new Vector2(100.0f, windowSize.Y));

            if (ImGui.Begin("##SIDEWIN"u8, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDecoration))
            {

            }
            ImGui.End();
        }
    }

    internal interface IWindow
    {
        public string WindowName { get; }

        public void Render();
    }
}
