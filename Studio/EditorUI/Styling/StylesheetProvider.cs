using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using EditorUI.Serialization;
using Primary.Collections.ReadOnly;
using Primary.Rendering.Assets;
using Primary.Utility;

namespace EditorUI.Styling
{
    public sealed class StylesheetProvider
    {
        private readonly ValueSerializer _valueSerializer;

        private List<Stylesheet> _stylesheets;

        private Dictionary<ClassKey, ClassList> _styleClasses;
        private Dictionary<ClassStyleKey, LazyStyleValue> _styleValues;

        private HashSet<ClassKey> _invalidClasses;

        internal StylesheetProvider(ValueSerializer valueSerializer)
        {
            _valueSerializer = valueSerializer;

            _stylesheets = new List<Stylesheet>();

            _styleClasses = new Dictionary<ClassKey, ClassList>();
            _styleValues = new Dictionary<ClassStyleKey, LazyStyleValue>();

            _invalidClasses = new HashSet<ClassKey>();
        }

        public void AddStylesheet(Stylesheet stylesheet)
        {
            if (_stylesheets.AddUnique(stylesheet))
            {
                foreach (var (key, stylesheetClass) in stylesheet.Classes)
                {
                    ref ClassList classes = ref CollectionsMarshal.GetValueRefOrAddDefault(_styleClasses, key, out bool exists);
                    if (!exists)
                    {
                        classes = new ClassList([stylesheetClass], 1);
                    }
                    else
                    {
                        classes.AddAtBack(stylesheetClass);
                    }

                    _invalidClasses.Add(key);
                }

                InvalidateAllFromClasses(stylesheet);
            }
        }

        public void RemoveStylesheet(Stylesheet stylesheet)
        {
            if (_stylesheets.Remove(stylesheet))
            {
                foreach (var (key, stylesheetClass) in stylesheet.Classes)
                {
                    ref ClassList classes = ref CollectionsMarshal.GetValueRefOrNullRef(_styleClasses, key);
                    if (!Unsafe.IsNullRef(in classes))
                    {
                        classes.RemoveClass(stylesheetClass);
                        _invalidClasses.Add(key);
                    }
                }
            }
        }

        public void ClearStylesheets()
        {
            _stylesheets.Clear();

            _styleClasses.Clear();
            _styleValues.Clear();
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

                    foreach (var (key, stylesheetClass) in stylesheet.Classes)
                    {
                        ref ClassList classes = ref CollectionsMarshal.GetValueRefOrAddDefault(_styleClasses, key, out bool exists);
                        if (!exists)
                        {
                            classes = new ClassList([stylesheetClass], 1);
                        }
                        else
                        {
                            classes.FocusClass(stylesheetClass);
                        }

                        _invalidClasses.Add(key);
                    }
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
                if (key.Class.TryGetStyleValue(key.Key, out string? value))
                {
                    if (_valueSerializer.TryDeserialize(type, value, out object? boxedValue, out Exception? exception))
                    {
                        styleValue = new LazyStyleValue(value, i);
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

        public bool TryGetClasses(ClassKey key, [NotNullWhen(true)] out ArraySegment<StylesheetClass> stylesheetClass)
        {
            if (_styleClasses.TryGetValue(key, out ClassList classList))
            {
                stylesheetClass = new ArraySegment<StylesheetClass>(classList.Classes, 0, classList.Length);
                return true;
            }

            stylesheetClass = ArraySegment<StylesheetClass>.Empty;
            return false;
        }

        public bool TryGetClassValue(ClassStyleKey styleKey, Type type, out object? value)
        {
            value = default;

            ref LazyStyleValue styleValue = ref CollectionsMarshal.GetValueRefOrNullRef(_styleValues, styleKey);
            if (Unsafe.IsNullRef(in styleValue))
                return false;

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

        public ROList<Stylesheet> Stylesheets => _stylesheets;

        private record struct ClassList
        {
            public StylesheetClass[] Classes;
            public int Length;

            public ClassList(StylesheetClass[] classes, int length)
            {
                Classes = classes;
                Length = length;
            }

            public void AddAtBack(StylesheetClass @class)
            {
                if (Classes.Length == Length)
                    Array.Resize(ref Classes, Classes.Length * 2);
                Classes[Length++] = @class;
            }

            public void RemoveClass(StylesheetClass @class)
            {
                int index = Array.IndexOf(Classes, @class);
                if (index != -1)
                {
                    if (Length == 1)
                    {
                        Classes[0] = null!;
                    }
                    else if (index == Length - 1)
                    {
                        Classes[Length - 1] = null!;
                    }
                    else
                    {
                        Array.Copy(Classes, index + 1, Classes, index, Length - index);
                        Classes[Length - 1] = null!;
                    }

                    --Length;
                }
            }

            public void FocusClass(StylesheetClass @class)
            {
                int index = Array.IndexOf(Classes, @class);

                if (index == -1)
                {
                    AddAtBack(@class);
                }
                else if (index < Length - 1)
                {
                    Array.Copy(Classes, index + 1, Classes, index, Length - index - 1);
                    Classes[Length - 1] = @class;
                }
            }
        }
    }

    internal readonly record struct LazyStyleValue(object? Value, int SourceIndex)
    {
        public const int SourceIndexUninitialized = -1;
        public const int SourceIndexBad = -2;
    }
}
