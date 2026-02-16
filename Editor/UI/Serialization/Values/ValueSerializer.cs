using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.UI.Serialization.Values
{
    internal interface IGenericValueSerializer
    {
        public string? Serialize<T>(T value);
        public bool Deserialize<T>(string value, out T? deserialized);
    }

    internal abstract class ValueSerializer<T> : IGenericValueSerializer
    {
        public abstract string? Serialize(T? value);
        public abstract bool Deserialize(string value, out T? deserialized);

        public string? Serialize<T1>(T1? value)
        {
            Debug.Assert(typeof(T) == typeof(T1));
            return Serialize(value == null ? default : Unsafe.As<T1, T>(ref value));
        }

        public bool Deserialize<T1>(string value, out T1? deserialized)
        {
            Debug.Assert(typeof(T) == typeof(T1));
            if (!Deserialize(value, out T? tmp))
            {
                deserialized = default;
                return false;
            }

            deserialized = tmp == null ? default : Unsafe.As<T, T1>(ref tmp);
            return true;
        }
    }
}
