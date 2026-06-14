using Editor.UI.Assets;
using Editor.UI.Styling;
using Hexa.NET.ImGui;
using Primary.Assets.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.DearImGui
{
    internal sealed class UIStylesheetEditor : IDearImGuiPopup
    {
        private StylesheetAsset _stylesheet;
        private int _loadIndex;

        private Dictionary<string, List<(string, object)>> _stateProperties;
        private List<(string, List<(string, object)>)> _usedStateProperties;
        private HashSet<string> _usedStates;

        public UIStylesheetEditor(StylesheetAsset stylesheet)
        {
            _stylesheet = stylesheet;
            _loadIndex = stylesheet.LoadIndex;

            _stateProperties = new Dictionary<string, List<(string, object)>>();
            _usedStateProperties = new List<(string, List<(string, object)>)>();
            _usedStates = new HashSet<string>();
        }

        public void Render(ref bool windowOpen)
        {
            if (ImGui.Begin($"{_stylesheet.Name} - Stylesheet", ref windowOpen))
            {
                if (_stylesheet.Status == ResourceStatus.Success)
                {
                    if (ImGui.BeginChild(2, ImGuiChildFlags.Borders))
                    {
                        foreach (var (key, classData) in _stylesheet.Classes)
                        {
                            if (ImGui.TreeNode(key))
                            {
                                HandleClassData(classData);
                                ImGui.TreePop();
                            }
                        }

                        ImGui.EndChild();
                    }
                }
                else
                {
                    ImGui.Text($"Stylesheet is not editable because of it's state: {_stylesheet.Status}");
                }
            }
            ImGui.End();
        }

        public void OpenPopup() { }

        private void HandleClassData(StylesheetClass classData)
        {
            foreach (var (key, value) in classData.Values)
            {
                if (_stateProperties.TryGetValue(key.StateName, out List<(string, object)>? list))
                {
                    list.Add((key.PropertyName, value));

                    if (_usedStates.Add(key.StateName))
                        _usedStateProperties.Add((key.StateName, list));
                }
                else
                {
                    list = [(key.PropertyName, value)];

                    _stateProperties.Add(key.StateName, list);
                    _usedStates.Add(key.StateName);
                    _usedStateProperties.Add((key.StateName, list));
                }
            }

            foreach (var (stateName, list) in _usedStateProperties)
            {
                if (ImGui.TreeNode(stateName))
                {
                    ImGui.Indent();
                    foreach (var (key, value) in list)
                    {
                        if (ImGui.Selectable($"{key} = {value}"))
                        {

                        }
                    }
                    ImGui.Unindent();

                    ImGui.TreePop();
                }

                list.Clear();
            }

            _usedStateProperties.Clear();
            _usedStates.Clear();
        }

        public DearImGuiPopupFlags Flags => DearImGuiPopupFlags.None;

        public StylesheetAsset Stylesheet => _stylesheet;
    }
}
