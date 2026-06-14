using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using EditorUI.Reflection;
using Primary.Collections.ReadOnly;
using Primary.Rendering.Assets;

namespace EditorUI.Styling
{
    public sealed class Stylesheet
    {
        private readonly string _sourceName;

        private readonly Dictionary<ClassKey, StylesheetClass> _classes;

        public Stylesheet(string sourceName)
        {
            _sourceName = sourceName;

            _classes = new Dictionary<ClassKey, StylesheetClass>();
        }

        public StylesheetClass CreateClass(ClassType classType, string name)
        {
            Guard.IsFalse(classType == ClassType.Pseudo, "A pseudo class must be parented to another class");
            Guard.IsTrue(IsNameValid(classType, name), "Name is not valid for a class");

            StylesheetClassName className = new StylesheetClassName(classType, name);
            StylesheetClass stylesheetClass = new StylesheetClass(className);

            Guard.IsTrue(_classes.TryAdd(new ClassKey(classType, name), stylesheetClass), "Class with the same qualified name already exists");
            return stylesheetClass;
        }

        public StylesheetClass CreateOrGetClass(ClassType classType, string name, out bool alreadyExists)
        {
            Guard.IsFalse(classType == ClassType.Pseudo, "A pseudo class must be parented to another class");
            Guard.IsTrue(IsNameValid(classType, name), "Name is not valid for a class");

            ClassKey key = new ClassKey(classType, name);
            alreadyExists = _classes.TryGetValue(key, out StylesheetClass? stylesheetClass);

            if (!alreadyExists)
            {
                StylesheetClassName className = new StylesheetClassName(classType, name);
                stylesheetClass = new StylesheetClass(className);

                _classes.Add(key, stylesheetClass);
            }

            return stylesheetClass!;
        }

        public void RemoveClass(ClassType classType, string name)
        {
            _classes.Remove(new ClassKey(classType, name));
        }

        public bool TryGetClass(ClassType classType, string name, [NotNullWhen(true)] out StylesheetClass? stylesheetClass)
        {
            return _classes.TryGetValue(new ClassKey(classType, name), out stylesheetClass);
        }

        public string SourceName => _sourceName;

        public RODictionary<ClassKey, StylesheetClass> Classes => _classes;

        public static bool IsNameValid(ClassType classType, string name)
        {
            switch (classType)
            {
                case ClassType.Named:
                    {
                        if (name.Length < 2)
                            return false;

                        for (int i = 0; i < name.Length; i++)
                        {
                            char c = name[i];

                            if (i == 0)
                            {
                                if (c != '.')
                                    return false;
                            }
                            else
                            {
                                if (!char.IsLetterOrDigit(c) && c != '-')
                                    return false;
                            }
                        }

                        return true;
                    }
                case ClassType.Typed:
                    {
                        if (name.Length < 1)
                            return false;

                        for (int i = 0; i < name.Length; i++)
                        {
                            char c = name[i];

                            if (!char.IsLetterOrDigit(c) && c != '-')
                                return false;
                        }

                        return true;
                    }
                default: return false;
            }
        }

        public static string GetQualifiedClassName(ClassType classType, string name)
        {
            switch (classType)
            {
                case ClassType.Named: return $".{name}";
                case ClassType.Typed: return name;
                default: return string.Empty;
            }
        }
    }

    public readonly record struct ClassKey(ClassType Type, string Name)
    {
        public override string ToString() => $"{{ {Name}({Type}) }}";

        public override int GetHashCode() => HashCode.Combine(Type, Name.GetDjb2HashCode());
    }

    public readonly record struct ClassStyleKey(StylesheetClass Class, StyleKey Key)
    {
        public override string ToString() => $"{{ {Class.ClassName}={Key} }}";

        public override int GetHashCode() => HashCode.Combine(Class, Key.Property.GetDjb2HashCode(), Key.TriggerMask);
    }

    public readonly record struct StyleKey(string Property, ushort TriggerMask) : IEquatable<StyleKey>
    {
        public override string ToString() => $"{{ {Property} : {TriggerMask:x4} }}";

        public override int GetHashCode() => HashCode.Combine(Property.GetDjb2HashCode(), TriggerMask);
    }
}
