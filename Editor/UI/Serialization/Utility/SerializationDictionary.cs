using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Editor.UI.Serialization.Utility
{
    internal sealed class SerializationDictionary<TValue> where TValue : ISerializationType
    {
        private Dictionary<Type, TValue> _dictionary;
        private Dictionary<string, Type> _mappedTypes;

        internal SerializationDictionary()
        {
            _dictionary = new Dictionary<Type, TValue>();
            _mappedTypes = new Dictionary<string, Type>();
        }

        public void Add(TValue value)
        {
            Type type = value.Type;

            _dictionary.Add(type, value);
            _mappedTypes.Add(value.PrettyName, type);
        }

        public bool TryAdd(TValue value)
        {
            Type type = value.Type;

            if (_dictionary.TryAdd(type, value))
            {
                _mappedTypes.Add(value.PrettyName, type);
                return true;
            }

            return false;
        }

        public bool Remove(TValue value)
        {
            Type type = value.Type;

            bool ret = true;
            ret = _dictionary.Remove(type);
            ret = _mappedTypes.Remove(value.PrettyName) && ret;

            return ret;
        }

        public bool TryGetValue(Type type, [NotNullWhen(true)] out TValue? value) => _dictionary.TryGetValue(type, out value);
        public bool TryGetTyping(string prettyName, [NotNullWhen(true)] out Type? type) => _mappedTypes.TryGetValue(prettyName, out type);
    }
}
