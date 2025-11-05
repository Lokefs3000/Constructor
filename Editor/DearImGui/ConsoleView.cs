using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Enumerables;
using Hexa.NET.ImGui;
using Primary.Console;
using System.Text;

namespace Editor.DearImGui
{
    internal sealed class ConsoleView
    {
        private Dictionary<string, CVarGroup> _variableGroups;

        private string _currentConsoleInput;

        internal ConsoleView()
        {
            _variableGroups = new Dictionary<string, CVarGroup>();

            _currentConsoleInput = string.Empty;

            foreach (string varName in ConsoleManager.Variables)
            {
                ReadOnlySpan<char> roSpan = varName.AsSpan();
                ReadOnlySpanTokenizer<char> tokenizer = roSpan.Tokenize('_');

                tokenizer.MoveNext();

                string groupKey = tokenizer.Current.ToString();
                if (!_variableGroups.TryGetValue(groupKey, out CVarGroup group))
                {
                    group = new CVarGroup(groupKey);
                    _variableGroups.Add(groupKey, group);
                }

                group.Variables.Add(new CVarData("##" + varName, ConsoleManager.GetGenericValue(varName)!.ToString()!));
            }
        }

        internal void Render()
        {
            if (ImGui.Begin("Console"u8))
            {
                if (ImGui.InputText("##CLINE"u8, ref _currentConsoleInput, 256, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
                {
                    ConsoleManager.Process(_currentConsoleInput);
                }

                foreach (CVarGroup group in _variableGroups.Values)
                {
                    if (ImGui.CollapsingHeader(group.Name, ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        ImGui.Indent();

                        ImGui.BeginTable("##"u8, 2);

                        foreach (CVarData data in group.Variables)
                        {
                            string tempString = data.Value;

                            ImGui.TableNextRow();

                            ImGui.TableSetColumnIndex(0);
                            if (ImGui.Selectable(data.Name.AsSpan(2)))
                                _currentConsoleInput = Encoding.UTF8.GetString(data.Name.AsSpan(2)) + " ";
                            
                            ImGui.TableSetColumnIndex(1);
                            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                            if (ImGui.InputText(data.Name, ref tempString, 24, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
                                ConsoleManager.Process($"{Encoding.UTF8.GetString(data.Name.AsSpan(2))} {tempString}");
                        }

                        ImGui.EndTable();

                        ImGui.Unindent();
                    }
                }
            }
            ImGui.End();
        }

        private struct CVarGroup
        {
            public byte[] Name;
            public List<CVarData> Variables;

            public CVarGroup(string name)
            {
                Name = Encoding.UTF8.GetBytes(name);
                Variables = new List<CVarData>();
            }
        }

        private struct CVarData
        {
            public byte[] Name;
            public string Value;

            public CVarData(string name, string value)
            {
                Name = Encoding.UTF8.GetBytes(name);
                Value = value;
            }
        }
    }
}
