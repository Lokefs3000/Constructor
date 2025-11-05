using Editor.DearImGui.Debuggers;
using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui
{
    internal sealed class DebugView
    {
        private bool _isOpen;

        private List<IVisualDebugger> _debuggers;
        private int _activeDebuggerIndex;

        internal DebugView()
        {
            _isOpen = false;

            _debuggers = [
                new DynAtlasDebugger(),
                new ToolDebugger(),
                new AssetDebugger(),
                new RenderDebugger()
                ];
            _activeDebuggerIndex = -1;
        }

        internal void MenuBar()
        {
            if (ImGui.BeginMenu("Debug"u8))
            {
                if (ImGui.MenuItem("Open", _isOpen))
                {
                    _isOpen = true;
                }

                if (ImGui.BeginMenu("Engine"u8))
                {
                    for (int i = 0; i < _debuggers.Count; i++)
                    {
                        if (_debuggers[i].DebuggerType == VisualDebuggerType.Engine)
                        {
                            if (ImGui.MenuItem(_debuggers[i].DebuggerName, _activeDebuggerIndex == i))
                            {
                                _activeDebuggerIndex = i;
                                _isOpen = true;
                            }
                        }
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Editor"u8))
                {
                    for (int i = 0; i < _debuggers.Count; i++)
                    {
                        if (_debuggers[i].DebuggerType == VisualDebuggerType.Editor)
                        {
                            if (ImGui.MenuItem(_debuggers[i].DebuggerName, _activeDebuggerIndex == i))
                            {
                                _activeDebuggerIndex = i;
                                _isOpen = true;
                            }
                        }
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenu();
            }
        }

        internal void Render()
        {
            if (_isOpen)
            {
                if (ImGui.Begin("Debug view"u8, ref _isOpen))
                {
                    if (ImGui.BeginChild("##LIST"u8, new Vector2(MathF.Min(ImGui.GetContentRegionAvail().X * 0.5f, 125.0f), 0.0f), ImGuiChildFlags.Borders))
                    {
                        if (ImGui.Selectable("Default"u8, _activeDebuggerIndex == -1))
                        {
                            _activeDebuggerIndex = -1;
                        }

                        for (int i = 0; i < _debuggers.Count; i++)
                        {
                            if (ImGui.Selectable(_debuggers[i].DebuggerName, i == _activeDebuggerIndex))
                            {
                                _activeDebuggerIndex = i;
                            }
                        }

                    }
                    ImGui.EndChild();

                    ImGui.SameLine();

                    if (ImGui.BeginChild("##VIEW"u8, ImGuiChildFlags.Borders, ImGuiWindowFlags.MenuBar))
                    {
                        if (_activeDebuggerIndex == -1)
                        {
                            ImGui.TextUnformatted("No debugger active/selected."u8);
                        }
                        else
                        {
                            _debuggers[_activeDebuggerIndex].Render();
                        }

                    }
                    ImGui.EndChild();
                }
                ImGui.End();
            }
        }
    }
}
