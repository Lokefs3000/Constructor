using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.HighPerformance;
using Primary.Collections.ReadOnly;
using Primary.Common;

namespace EditorUI.Styling
{
    public sealed class StylesheetClass
    {
        private readonly StylesheetClassName _className;

        private readonly Dictionary<StyleKey, StyleValue[]> _keys;
        private readonly List<StylesheetClass> _subClasses;

        internal StylesheetClass(StylesheetClassName className)
        {
            _className = className;

            _keys = new Dictionary<StyleKey, StyleValue[]>();
            _subClasses = new List<StylesheetClass>();
        }

        public StylesheetClass CreateSubClass(PseduoClassType pseduoClass, string typeName = "")
        {
            switch (pseduoClass)
            {
                case PseduoClassType.FirstChild:
                case PseduoClassType.LastChild:
                case PseduoClassType.OnlyChild:
                case PseduoClassType.AllChildren:
                    {
                        Guard.IsEmpty(typeName, "No type name should be provided for typeless selectors");
                        break;
                    }
                case PseduoClassType.FirstOfType:
                case PseduoClassType.LastOfType:
                case PseduoClassType.OnlyOfType:
                    {
                        Guard.IsNotEmpty(typeName, "Type name should be provided for typed selectors");
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

        public void SetStyleValue(StyleKey key, string value, int triggerIndex = -1)
        {
            ref StyleValue[]? styleValues = ref CollectionsMarshal.GetValueRefOrAddDefault(_keys, key, out bool exists);
            if (!exists)
            {
                styleValues = [new StyleValue(value, triggerIndex)];
            }
            else
            {
                for (int i = 0; i < styleValues!.Length; ++i)
                {
                    ref StyleValue thisStyleValue = ref styleValues[i];
                    if (thisStyleValue.TriggerIndex == triggerIndex)
                    {
                        thisStyleValue = new StyleValue(value, triggerIndex);
                        break;
                    }
                    else if (thisStyleValue.TriggerIndex > triggerIndex)
                    {
                        Array.Resize(ref styleValues, styleValues.Length + 1);
                        Array.Copy(styleValues, i, styleValues, i + 1, styleValues.Length - i - 1);

                        styleValues[i] = new StyleValue(value, triggerIndex);
                        break;
                    }
                }

                Array.Resize(ref styleValues, styleValues.Length + 1);
                styleValues[^1] = new StyleValue(value, triggerIndex);
            }
        }

        public void RemoveStyleValue(StyleKey key, int triggerIndex = -1)
        {
            ref StyleValue[] styleValues = ref CollectionsMarshal.GetValueRefOrNullRef(_keys, key);
            if (!Unsafe.IsNullRef(in styleValues))
            {
                for (int i = 0; i < styleValues!.Length; ++i)
                {
                    ref StyleValue thisStyleValue = ref styleValues[i];
                    if (thisStyleValue.TriggerIndex == triggerIndex)
                    {
                        if (i < styleValues.Length - 1)
                            Array.Copy(styleValues, i + 1, styleValues, i, styleValues.Length - i - 1);
                        else if (i == 0 && styleValues.Length == 1)
                            _keys.Remove(key);

                        Array.Resize(ref styleValues, styleValues.Length - 1);
                        break;
                    }
                }
            }
        }

        public bool HasStyleValue(StyleKey key)
        {
            return _keys.ContainsKey(key);
        }

        public bool TryGetStyleValue(StyleKey key, ushort triggerMask, [NotNullWhen(true)] out string? value)
        {
            ref StyleValue[] styleValues = ref CollectionsMarshal.GetValueRefOrNullRef(_keys, key);
            if (Unsafe.IsNullRef(in styleValues))
            {
                value = null;
                return false;
            }

            if (triggerMask == 0)
            {
                StyleValue firstValue = styleValues[0];
                if (firstValue.TriggerIndex == -1)
                {
                    value = firstValue.Value;
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
            else
            {
                int maxIndex = sizeof(ushort) * 8 - 1 - ushort.LeadingZeroCount(triggerMask);
                for (int i = maxIndex; i >= 0; --i)
                {
                    StyleValue currentValue = styleValues[i];
                    if (currentValue.TriggerIndex != -1 && Flags.HasFlag(triggerMask, 1 << currentValue.TriggerIndex))
                    {
                        value = currentValue.Value;
                        return true;
                    }
                }

                value = null;
                return false;
            }
        }

        public bool TryGetStyleValueIndex(StyleKey key, int triggerIndex, [NotNullWhen(true)] out string? value)
        {
            ref StyleValue[] styleValues = ref CollectionsMarshal.GetValueRefOrNullRef(_keys, key);
            if (Unsafe.IsNullRef(in styleValues))
            {
                value = null;
                return false;
            }

            if (triggerIndex == -1)
            {
                StyleValue firstValue = styleValues[0];
                if (firstValue.TriggerIndex == -1)
                {
                    value = firstValue.Value;
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
            else
            {
                for (int i = Math.Min(styleValues.Length, triggerIndex + 2) - 1; i >= 0; --i)
                {
                    StyleValue currentValue = styleValues[i];
                    if (currentValue.TriggerIndex == triggerIndex)
                    {
                        value = currentValue.Value;
                        return true;
                    }
                }

                value = null;
                return false;
            }
        }

        public bool TryGetTriggerIndex(StyleKey key, ushort triggerMask, out int triggerIndex)
        {
            ref StyleValue[] styleValues = ref CollectionsMarshal.GetValueRefOrNullRef(_keys, key);
            if (Unsafe.IsNullRef(in styleValues))
            {
                triggerIndex = -1;
                return false;
            }

            if (triggerMask == 0)
            {
                StyleValue firstValue = styleValues[0];
                if (firstValue.TriggerIndex == -1)
                {
                    triggerIndex = -1;
                    return true;
                }
                else
                {
                    triggerIndex = -1;
                    return false;
                }
            }
            else
            {
                int maxIndex = sizeof(ushort) * 8 - ushort.LeadingZeroCount(triggerMask);
                for (int i = Math.Min(styleValues.Length - 1, maxIndex); i >= 0; --i)
                {
                    StyleValue currentValue = styleValues[i];
                    if (currentValue.TriggerIndex == -1 || Flags.HasFlag(triggerMask, 1 << currentValue.TriggerIndex))
                    {
                        triggerIndex = currentValue.TriggerIndex;
                        return true;
                    }
                }

                triggerIndex = -1;
                return false;
            }
        }

        public override string ToString()
        {
            return _className.ToString();
        }

        public StylesheetClassName ClassName => _className;

        public ROList<StylesheetClass> SubClasses => _subClasses;

        private readonly record struct StyleValue(string Value, int TriggerIndex);
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

        public override string ToString() => _type == ClassType.Pseudo ? (string.IsNullOrEmpty(_name) ? $"{{ {_pseduoType} }}" : $"{{ {_pseduoType}={_name} }}") : $"{{ {_type}={_name} }}";

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
        OnlyOfType,

        AllChildren,
    }
}
