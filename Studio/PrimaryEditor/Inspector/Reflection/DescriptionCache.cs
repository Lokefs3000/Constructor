using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Collections;
using Primary.Editor;
using PrimaryEditor.Inspector.Setup;

namespace PrimaryEditor.Inspector.Reflection
{
    public sealed class DescriptionCache
    {
        private readonly ValueSourceGenerator _valueSourceGenerator;
        private Dictionary<Type, InspectorDescription?> _descriptions;

        internal DescriptionCache(ValueSourceGenerator valueSourceGenerator)
        {
            _valueSourceGenerator = valueSourceGenerator;
            _descriptions = new Dictionary<Type, InspectorDescription?>();
        }

        private InspectorDescription SetupDescriptions(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            using RentedList<DescriptionValue> values = new RentedList<DescriptionValue>(fields.Length);
            foreach (FieldInfo field in fields)
            {
                if (field.IsPublic)
                {
                    if (field.GetCustomAttribute<InspectorHiddenAttribute>() != null)
                        continue;

                    Type fieldType = field.FieldType;
                    bool needsDescriptions = !s_builtInTypes.Contains(fieldType) && !fieldType.IsEnum;

                    InspectorValueSource valueSource = _valueSourceGenerator.GetValueSource(field);

                    if (needsDescriptions)
                    {
                        if (TryGetDescription(fieldType, out InspectorDescription? valueDesc))
                        {
                            values.Add(new DescriptionValue(valueDesc, fieldType, valueSource));
                        }
                    }
                    else
                    {
                        values.Add(new DescriptionValue(null, fieldType, valueSource));
                    }
                }
            }

            return new InspectorDescription(type, [.. values]);
        }

        internal bool TryGetDescription(Type type, [NotNullWhen(true)] out InspectorDescription? description)
        {
            ref InspectorDescription? descRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_descriptions, type, out bool exists);
            if (!exists)
            {
                try
                {
                    descRef = SetupDescriptions(type);
                }
                catch (Exception ex)
                {
                    EdLog.Inspector.Error(ex, "Failed to setup descriptions for type '{t}'", type);
                    descRef = null;
                }
            }

            description = descRef;
            return description != null;
        }

        private static readonly FrozenSet<Type> s_builtInTypes = new HashSet<Type>
        {
            typeof(bool),
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(string),
        }.ToFrozenSet();
    }
}
