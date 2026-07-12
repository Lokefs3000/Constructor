using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using EditorUI.Reflection;
using EditorUI.Serialization;
using Primary.Collections;
using Primary.Collections.ReadOnly;
using Primary.Rendering.Assets;
using Primary.Utility;

namespace EditorUI.Styling
{
    public sealed class StylesheetProvider : IDisposable
    {
        private readonly ValueSerializer _valueSerializer;

        private List<Stylesheet> _stylesheets;
        private Dictionary<ClassStyleKey, LazyStyleValue> _styleValues;

        private HashSet<ClassKey> _invalidClasses;

        private bool _hasChangedStylesheets;

        private bool _disposedValue;

        internal StylesheetProvider(ValueSerializer valueSerializer)
        {
            _valueSerializer = valueSerializer;

            _stylesheets = new List<Stylesheet>();
            _styleValues = new Dictionary<ClassStyleKey, LazyStyleValue>();

            _invalidClasses = new HashSet<ClassKey>();

            _hasChangedStylesheets = false;

            UIManager.Instance.StyleManager.RegisterProvider(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    UIManager.Instance.StyleManager.UnregisterProvider(this);
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void OnStylesUpdated()
        {
            _hasChangedStylesheets = false;
            _invalidClasses.Clear();
        }

        internal void TryReplaceStylesheet(string sourceName, Stylesheet newStylesheet)
        {
            int index = _stylesheets.FindIndex((x) => x.SourceName == sourceName);
            if (index != -1)
            {
                _stylesheets[index] = newStylesheet;
                _styleValues.Clear();

                _hasChangedStylesheets = true;
            }
        }

        public void AddStylesheet(Stylesheet stylesheet)
        {
            if (_stylesheets.AddUnique(stylesheet))
            {
                InvalidateAllFromClasses(stylesheet);
                _hasChangedStylesheets = true;
            }
        }

        public void RemoveStylesheet(Stylesheet stylesheet)
        {
            _stylesheets.Remove(stylesheet);
            _styleValues.Clear();

            _hasChangedStylesheets = true;
        }

        public void ClearStylesheets()
        {
            _stylesheets.Clear();
            _styleValues.Clear();

            _hasChangedStylesheets = true;
        }

        public bool TryFocusStylesheet(Stylesheet stylesheet)
        {
            int index = _stylesheets.IndexOf(stylesheet);
            if (index != -1)
            {
                if (index < _stylesheets.Count - 1)
                {
                    _stylesheets.RemoveAt(index);
                    _stylesheets.Add(stylesheet);

                    _hasChangedStylesheets = true;
                }

                return true;
            }

            return false;
        }

        // public bool TryMoveStylesheet(Stylesheet stylesheet, int destinationIndex)
        // {
        //     Guard.IsInRange(destinationIndex, 0, _stylesheets.Count);
        // 
        //     int index = _stylesheets.IndexOf(stylesheet);
        //     if (index != -1)
        //     {
        //         if (index != destinationIndex)
        //         {
        //             (_stylesheets[index], _stylesheets[destinationIndex]) = (_stylesheets[destinationIndex], _stylesheets[index]);
        // 
        //             InvalidateAllClasses(stylesheet);
        //         }
        // 
        //         return true;
        //     }
        // 
        //     return false;
        // }

        public int TryFindStylesheetIndex(Stylesheet stylesheet)
        {
            for (int i = 0; i < _stylesheets.Count; i++)
            {
                if (_stylesheets[i] == stylesheet)
                    return i;
            }

            return -1;
        }

        private void InvalidateAllFromClasses(Stylesheet stylesheet)
        {
            foreach (var (key, stylesheetClass) in stylesheet.Classes)
            {
                _invalidClasses.Add(key);
            }
        }

        private bool TryResolveStyleValue(ClassStyleKey key, Type type, ref LazyStyleValue styleValue)
        {
            Debug.Assert(styleValue.SourceIndex == LazyStyleValue.SourceIndexUninitialized);

            for (int i = _stylesheets.Count - 1; i >= 0; --i)
            {
                if (key.Class.TryGetStyleValueIndex(key.Key, key.TriggerMask, out string? value))
                {
                    if (_valueSerializer.TryDeserialize(type, value, out object? boxedValue, out Exception? exception))
                    {
                        styleValue = new LazyStyleValue(boxedValue, i);
                        return true;
                    }
                    else
                    {
                        UILog.Logger?.Warning(exception, "Error occured in deserializing value {v} in stylesheet class {stcl}", value, key.Class.ClassName);
                    }
                }
            }

            styleValue = new LazyStyleValue(null, LazyStyleValue.SourceIndexBad);

            UILog.Logger?.Error("Failed to resolve style value for key {k} (type: {t})", key, type);
            return false;
        }

        public bool TryGetClassValue(ClassStyleKey styleKey, Type type, out object? value)
        {
            value = default;
            if (!styleKey.Class.TryGetTriggerIndex(styleKey.Key, (ushort)styleKey.TriggerMask, out int triggerIndex))
                return false;

            styleKey = new ClassStyleKey(styleKey.Class, styleKey.Key, (ushort)triggerIndex);

            ref LazyStyleValue styleValue = ref CollectionsMarshal.GetValueRefOrAddDefault(_styleValues, styleKey, out bool exists);
            if (!exists)
                styleValue = new LazyStyleValue(null, LazyStyleValue.SourceIndexUninitialized);

            switch (styleValue.SourceIndex)
            {
                case LazyStyleValue.SourceIndexUninitialized:
                    {
                        if (TryResolveStyleValue(styleKey, type, ref styleValue))
                            goto default;
                        else
                            return false;
                    }
                case LazyStyleValue.SourceIndexBad:
                    {
                        return false;
                    }
                default:
                    {
                        value = styleValue.Value;
                        return true;
                    }
            }
        }

        public bool TryGetClassValue(ClassStyleKey styleKey, PropertyData propertyData, out object? value)
        {
            value = default;
            if (!styleKey.Class.TryGetTriggerIndex(styleKey.Key, (ushort)styleKey.TriggerMask, out int triggerIndex))
                return false;

            styleKey = new ClassStyleKey(styleKey.Class, styleKey.Key, triggerIndex);

            ref LazyStyleValue styleValue = ref CollectionsMarshal.GetValueRefOrAddDefault(_styleValues, styleKey, out bool exists);
            if (!exists)
                styleValue = new LazyStyleValue(null, LazyStyleValue.SourceIndexUninitialized);

            switch (styleValue.SourceIndex)
            {
                case LazyStyleValue.SourceIndexUninitialized:
                    {
                        foreach (Type type in propertyData.PropertyTypes)
                        {
                            if (TryResolveStyleValue(styleKey, type, ref styleValue))
                                goto default;
                        }

                        styleValue = new LazyStyleValue(null, LazyStyleValue.SourceIndexBad);
                        return false;
                    }
                case LazyStyleValue.SourceIndexBad:
                    {
                        return false;
                    }
                default:
                    {
                        value = styleValue.Value;
                        return true;
                    }
            }
        }

        public ROList<Stylesheet> Stylesheets => _stylesheets;

        public bool HasChangedStylesheets => _hasChangedStylesheets;
    }

    internal readonly record struct LazyStyleValue(object? Value, int SourceIndex)
    {
        public const int SourceIndexUninitialized = -1;
        public const int SourceIndexBad = -2;
    }
}
