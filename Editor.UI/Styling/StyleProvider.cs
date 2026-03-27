using Editor.UI.Assets;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Editor.UI.Styling
{
    public sealed class StyleProvider
    {
        private List<StylesheetAsset> _stylesheets;
        private Dictionary<string, ClassData[]> _classes;

        internal StyleProvider()
        {
            _stylesheets = new List<StylesheetAsset>();
            _classes = new Dictionary<string, ClassData[]>();
        }

        public void AddStylesheet(StylesheetAsset asset)
        {
            if (_stylesheets.AddUnique(asset))
            {
                foreach (var kvp in asset.Classes)
                {
                    if (_classes.TryGetValue(kvp.Key, out ClassData[]? classes))
                    {
                        Array.Resize(ref classes, classes.Length + 1);
                        classes[classes.Length - 1] = new ClassData(asset, kvp.Value);
                    }
                    else
                    {
                        _classes.Add(kvp.Key, [new ClassData(asset, kvp.Value)]);
                    }
                }
            }
        }

        public void RemoveStylesheet(StylesheetAsset asset)
        {
            if (_stylesheets.Remove(asset))
            {
                foreach (var kvp in asset.Classes)
                {
                    if (_classes.TryGetValue(kvp.Key, out ClassData[]? classes))
                    {
                        int idx = Array.FindIndex(classes, (x) => x.Source == asset);
                        if (idx != -1)
                        {
                            if (classes.Length == 1)
                                _classes.Remove(kvp.Key);
                            else
                            {
                                if (idx < classes.Length - 1)
                                    Array.Copy(classes, idx + 1, classes, idx, classes.Length - idx);
                                Array.Resize(ref classes, classes.Length - 1);
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<StylesheetClass> GetClassesWithName(string className)
        {
            if (_classes.TryGetValue(className, out ClassData[]? value))
            {
                return value.Select((x) => x.Value).Reverse();
            }

            return Enumerable.Empty<StylesheetClass>();
        }

        public bool TryGetClassValue(string propertyName, IEnumerable<string> classList, [NotNullWhen(true)] out object? value)
        {
            foreach (string className in classList)
            {
                foreach (StylesheetClass @class in GetClassesWithName(className))
                {
                    if (@class.TryGetProperty(propertyName, out value))
                        return true;
                }
            }

            value = null;
            return false;
        }

        private readonly record struct ClassData(StylesheetAsset Source, StylesheetClass Value);
    }
}
