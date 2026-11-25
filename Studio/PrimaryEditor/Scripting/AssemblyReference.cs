using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PrimaryEditor.Scripting
{
    public abstract record class AssemblyReference
    {
        public sealed record class Named(AssemblyName Name) : AssemblyReference;
        public sealed record class Local(Guid Id) : AssemblyReference;
    }
}
