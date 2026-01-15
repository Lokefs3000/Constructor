namespace Editor.DearImGui.Debuggers
{
    internal interface IVisualDebugger
    {
        public void Render();

        public VisualDebuggerType DebuggerType { get; }
        public ReadOnlySpan<byte> DebuggerName { get; }
    }

    internal enum VisualDebuggerType : byte
    {
        Engine = 0,
        Editor
    }
}
