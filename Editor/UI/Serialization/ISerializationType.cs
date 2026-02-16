using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Serialization
{
    internal interface ISerializationType
    {
        public Type Type { get; }
        public string PrettyName { get; }
    }
}
