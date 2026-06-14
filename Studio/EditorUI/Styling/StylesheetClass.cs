using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Collections.ReadOnly;

namespace EditorUI.Styling
{
    public sealed class StylesheetClass
    {
        private readonly StylesheetClassName _className;

        private readonly Dictionary<StyleKey, string> _keys;
        private readonly List<StylesheetClass> _subClasses;

        internal StylesheetClass(StylesheetClassName className)
        {
            _className = className;

            _keys = new Dictionary<StyleKey, string>();
            _subClasses = new List<StylesheetClass>();
        }

        public StylesheetClass CreateSubClass(PseduoClassType pseduoClass, string typeName = "")
        {
            switch (pseduoClass)
            {
                case PseduoClassType.FirstChild:
                case PseduoClassType.LastChild:
                case PseduoClassType.OnlyChild:
                    {
                        Guard.IsNotEmpty(typeName, "No type name should be provided for typeless selectors");
                        break;
                    }
                case PseduoClassType.FirstOfType:
                case PseduoClassType.LastOfType:
                case PseduoClassType.OnlyOfType:
                    {
                        Guard.IsEmpty(typeName, "Type name should be provided for typed selectors");
                        break;
                    }
            }

            Guard.IsEqualTo(FindSubClassIndex(pseduoClass, typeName), -1, "A matching pseudo class already exists");

            StylesheetClassName className = new StylesheetClassName(pseduoClass, typeName);

            _subClasses.Add(new StylesheetClass(className));
            return _subClasses[^1];
        }

        public int FindSubClassIndex(PseduoClassType pseduoClass, string typeName = "")
        {
            for (int i = 0; i < _subClasses.Count; i++)
            {
                StylesheetClass stylesheetClass = _subClasses[i];
                StylesheetClassName className = stylesheetClass._className;

                if (className.PseduoType == pseduoClass)
                {
                    if (pseduoClass >= PseduoClassType.FirstOfType && typeName != string.Empty)
                    {
                        if (className.Name != typeName)
                            continue;
                    }

                    return i;
                }
            }

            return -1;
        }

        public void SetStyleValue(StyleKey key, string value)
        {
            _keys[key] = value;
        }

        public void RemoveStyleValue(StyleKey key)
        {
            _keys.Remove(key);
        }

        public bool HasStyleValue(StyleKey key)
        {
            return _keys.ContainsKey(key);
        }

        public bool TryGetStyleValue(StyleKey key, [NotNullWhen(true)] out string? value)
        {
            return _keys.TryGetValue(key, out value);
        }

        public StylesheetClassName ClassName => _className;

        public ROList<StylesheetClass> SubClasses => _subClasses;
    }

    public readonly record struct StylesheetClassName
    {
        private readonly ClassType _type;
        private readonly PseduoClassType _pseduoType;

        private readonly string _name;

        internal StylesheetClassName(ClassType classType, string name)
        {
            _type = classType;
            _pseduoType = 0;

            _name = name;
        }

        internal StylesheetClassName(PseduoClassType classType, string typeName)
        {
            _type = ClassType.Pseudo;
            _pseduoType = classType;

            _name = typeName;
        }

        public override string ToString() => _type == ClassType.Pseudo ? $"{{ {_pseduoType}={_name} }}" : $"{{ {_type}={_name} }}";

        public override int GetHashCode() => HashCode.Combine(_type, _pseduoType, _name.GetDjb2HashCode());

        public ClassType Type => _type;
        public PseduoClassType PseduoType => _pseduoType;

        public string Name => _name;
    }

    public enum ClassType : byte
    {
        /// <summary>The class has a name: ".class-name"</summary>
        Named = 0,

        /// <summary>The class is typed: "Label"</summary>
        Typed,

        /// <summary>The class a pseudo class: ":first"</summary>
        Pseudo
    }

    public enum PseduoClassType : byte
    {
        FirstChild = 0,
        LastChild,
        OnlyChild,

        FirstOfType,
        LastOfType,
        OnlyOfType
    }
}
