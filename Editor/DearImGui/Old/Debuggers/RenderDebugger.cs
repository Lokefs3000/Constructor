using Editor.Rendering.Debugging;
using Hexa.NET.ImGui;

namespace Editor.DearImGui.Debuggers
{
    internal class RenderDebugger : IVisualDebugger
    {
        public void Render()
        {
            BooleanToggle(1, "Draw entity bounds"u8, ref RenderDebug.DrawEntityBounds);
            BooleanToggle(2, "Draw render tree"u8, ref RenderDebug.DrawRenderTree);
            BooleanToggle(3, "Draw tree connections"u8, ref RenderDebug.DrawEntityTreeConnections);

            static void BooleanToggle(byte id, ReadOnlySpan<byte> text, ref bool value)
            {
                ImGui.TextUnformatted(text);
                ImGui.SameLine();
                ImGui.Checkbox(ref id, ref value);
            }
        }

        public VisualDebuggerType DebuggerType => VisualDebuggerType.Engine;
        public ReadOnlySpan<byte> DebuggerName => "Rendering"u8;
    }
}
