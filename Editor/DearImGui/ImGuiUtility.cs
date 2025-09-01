using Hexa.NET.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.DearImGui
{
    internal static class ImGuiUtility
    {
        public static unsafe ImTextureRef GetTextureRef(nint handle) => new ImTextureRef(null, new ImTextureID(handle));
    }
}
