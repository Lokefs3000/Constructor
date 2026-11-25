using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.UI.Styling
{
    public sealed class StylesheetClass
    {
        private bool _isStaged;
        private Dictionary<PropertyKey, object> _values;

        internal StylesheetClass(bool isStaged)
        {
            _isStaged = isStaged;
            _values = new Dictionary<PropertyKey, object>();
        }

        public void SetProperty(object? value, string propertyName, string stateName = "Normal")
        {
            if (value == null)
                _values.Remove(new PropertyKey(propertyName, stateName));
            else
                _values[new PropertyKey(propertyName, stateName)] = value;
        }

        public object GetProperty(string propertyName, string stateName = "Normal")
        {
            return _values[new PropertyKey(propertyName, stateName)];
        }

        public bool TryGetProperty(string propertyName, [NotNullWhen(true)] out object? value, string stateName = "Normal")
        {
            return _values.TryGetValue(new PropertyKey(propertyName, stateName), out value);
        }

        public void SetProperty<T>(T? value, string propertyName, string stateName = "Normal") where T : notnull
        {
            if (value == null)
                _values.Remove(new PropertyKey(propertyName, stateName));
            else
                _values[new PropertyKey(propertyName, stateName)] = value;
        }

        public T GetProperty<T>(string propertyName, string stateName = "Normal") where T : notnull
        {
            return (T)_values[new PropertyKey(propertyName, stateName)];
        }

        public bool TryGetProperty<T>(string propertyName, [NotNullWhen(true)] out T? value, string stateName = "Normal") where T : notnull
        {
            bool r = _values.TryGetValue(new PropertyKey(propertyName, stateName), out object? temp);
            value = (T?)temp;

            return r;
        }

        public bool HasProperty(string propertyName, string stateName = "Normal")
        {
            return _values.ContainsKey(new PropertyKey(propertyName, stateName));
        }

        public object? this[string propertyName, string stateName = "Normal"]
        {
            get => _values[new PropertyKey(propertyName, stateName)];
            set
            {
                if (value == null)
                    _values.Remove(new PropertyKey(propertyName, stateName));
                else
                    _values[new PropertyKey(propertyName, stateName)] = value;
            }
        }

        public bool IsStaged => _isStaged;
        public IReadOnlyDictionary<PropertyKey, object> Values => _values;
    }

    public readonly record struct PropertyKey(string PropertyName, string StateName)
    {
        public override int GetHashCode() => PropertyName.GetDjb2HashCode() ^ StateName.GetDjb2HashCode();
    }
}
