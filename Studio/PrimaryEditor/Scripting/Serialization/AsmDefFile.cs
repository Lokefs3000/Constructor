using System;
using System.Collections.Generic;
using System.Text;

namespace PrimaryEditor.Scripting.Serialization
{
    internal sealed class AsmDefFile
    {
        public Guid Id { get; set; } = Guid.AllBitsSet;
        public string AssemblyName { get; set; } = string.Empty;
        public string[] Symbols { get; set; } = [];
        public AsmDefReference[] References { get; set; } = [];
    }
}
