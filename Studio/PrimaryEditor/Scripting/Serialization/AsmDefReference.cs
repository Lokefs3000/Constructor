using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace PrimaryEditor.Scripting.Serialization
{
    [JsonPolymorphic]
    [JsonDerivedType(typeof(Named))]
    [JsonDerivedType(typeof(Local))]
    internal abstract class AsmDefReference
    {
        internal sealed class Named : AsmDefReference
        {
            public string FullName { get; set; } = string.Empty;
        }

        internal sealed class Local : AsmDefReference
        {
            public Guid Id { get; set; } = Guid.AllBitsSet;
        }
    }
}
