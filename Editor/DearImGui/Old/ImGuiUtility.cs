using Hexa.NET.ImGui;

namespace Editor.DearImGui
{
    internal static class ImGuiUtility
    {
        public static unsafe ImTextureRef GetTextureRef(nint handle) => new ImTextureRef(null, new ImTextureID(handle));
    }
}
