using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Interaction
{
    public sealed class ToolManager
    {
        public EditorTool Tool { get; set; } = EditorTool.Translate;
    }
    
    public enum EditorTool : byte
    {
        Translate,
        Rotate,
        Scale,
    }

    public enum EditorOriginMode : byte
    {
        Individual = 0,
        Center
    }
}
