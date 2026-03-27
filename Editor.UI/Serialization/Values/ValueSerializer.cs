using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.UI.Serialization.Values
{
    internal interface IValueSerializer<T> : IPrivateValueSerializer
    {
        //public string? Serialize(T value);
        //public bool Deserialize(string value, out T? deserialized);
    }

    internal interface IPrivateValueSerializer
    {

    }
}
