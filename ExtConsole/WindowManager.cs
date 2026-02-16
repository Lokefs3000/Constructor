using ExtConsole.Communication;
using ExtConsole.Communication.Messages;
using ExtConsole.Communication.Messages.General;
using ExtConsole.Communication.Net;
using ExtConsole.Windows;
using Hexa.NET.ImGui;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ExtConsole
{
    internal sealed class WindowManager
    {
        private ExtConsoleHost _host;

        private IWindow[] _windows;
        private int _currentWindow;

        internal WindowManager(ExtConsoleHost host)
        {
            _host = host;

            _windows = [
                new ProfilerWindow()
                ];

            _currentWindow = -1;

            host.MessageRecieved += Host_MessageRecieved;
        }

        private void Host_MessageRecieved(QueuedMessage queued)
        {
            if (_currentWindow != -1)
            {
                _windows[_currentWindow].Recieve(queued);
            }
        }

        public void Render(Vector2 windowSize, ExtConsoleHost host)
        {
            ImGuiContextPtr context = ImGui.GetCurrentContext();
            float barHeight = context.FontSize + context.Style.FramePadding.Y - 1.0f + 4.0f;

            if (host.ActiveClient != null)
            {
                ImGui.SetNextWindowPos(Vector2.Zero);
                ImGui.SetNextWindowSize(new Vector2(150.0f, windowSize.Y - barHeight - context.Style.FramePadding.Y));

                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10.0f));

                bool begin = ImGui.Begin("##SIDEWIN"u8, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDecoration);

                ImGui.PopStyleVar(2);

                if (begin)
                {
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    if (ImGui.BeginCombo("##CL"u8, "CLIENT"))
                    {
                        foreach (ConnectedClient client in host.Clients)
                        {
                            if (ImGui.Selectable("CLIENT"u8, client == host.ActiveClient))
                            {
                                host.ActiveClient = client;
                            }
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.NewLine();
                    ImGui.Separator();
                    ImGui.NewLine();

                    int i = 0;
                    foreach (IWindow window in _windows)
                    {
                        if (ImGui.Selectable(window.WindowName, _currentWindow == i))
                        {
                            if (_currentWindow == i)
                                _currentWindow = -1;
                            else
                                _currentWindow = i;

                            host.SendMessage(host.ActiveClient!, new MsgChangeComponent
                            {
                                Component = (MsgChangeComponent.ComponentId)_currentWindow
                            });
                        }

                        ++i;
                    }
                }
                ImGui.End();

                if (_currentWindow != -1)
                {
                    IWindow window = _windows[_currentWindow];

                    float offsetFromTop = 0.0f;

                    if (window.HasMenuBar)
                    {
                        offsetFromTop += barHeight + context.Style.FramePadding.Y;

                        ImGui.SetNextWindowPos(new Vector2(154.0f, 0.0f));
                        ImGui.SetNextWindowSize(new Vector2(windowSize.X - 154.0f, barHeight));

                        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

                        begin = ImGui.Begin("##MENUBAR"u8, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.MenuBar);

                        ImGui.PopStyleVar(2);

                        if (begin)
                        {
                            if (ImGui.BeginMenuBar())
                            {
                                window.MenuBar(host);
                                ImGui.EndMenuBar();
                            }
                        }
                        ImGui.End();
                    }

                    ImGui.SetNextWindowPos(new Vector2(154.0f, offsetFromTop));
                    ImGui.SetNextWindowSize(new Vector2(windowSize.X - 154.0f, windowSize.Y - offsetFromTop - barHeight - context.Style.FramePadding.Y));

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10.0f));

                    begin = ImGui.Begin("##MAINWIN"u8, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDecoration);

                    ImGui.PopStyleVar(2);

                    if (begin)
                    {
                        window.Render(host);
                    }
                    ImGui.End();
                }
            }
            else
            {
                ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();
                Vector2 textSize = ImGui.CalcTextSize("No clients currently connected..");

                drawList.AddText(new Vector2(windowSize.X, windowSize.Y - barHeight) * 0.5f - textSize * 0.5f, 0xffaaaaaa, "No clients currently connected..");
            }

            {
                ImGui.SetNextWindowPos(new Vector2(0.0f, windowSize.Y - barHeight));
                ImGui.SetNextWindowSize(new Vector2(windowSize.X, barHeight));

                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10.0f));

                bool begin = ImGui.Begin("##STATUS"u8, ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.MenuBar);

                ImGui.PopStyleVar(2);

                if (begin)
                {
                    if (ImGui.BeginMenuBar())
                    {
                        ImGuiIOPtr io = ImGui.GetIO();

                        ImGui.TextUnformatted($"Frametime: {io.DeltaTime.ToString("F3", CultureInfo.InvariantCulture)}s ({(int)io.Framerate})");
                        ImGui.TextUnformatted($"Clients: {host.Clients.Count}");
                        ImGui.TextUnformatted($"TrafficIn: {FileUtility.FormatSize(host.IncomingTraffic, provider: CultureInfo.InvariantCulture)}/t");
                        ImGui.TextUnformatted($"TrafficOut: {FileUtility.FormatSize(host.OutgoingTraffic, provider: CultureInfo.InvariantCulture)}/t");

                        ImGui.EndMenuBar();
                    }
                }
                ImGui.End();
            }
        }
    }

    internal interface IWindow
    {
        public string WindowName { get; }
        public bool HasMenuBar { get; }

        public void Render(ExtConsoleHost host);
        public void MenuBar(ExtConsoleHost host);

        public void Recieve(QueuedMessage queued);
    }
}
