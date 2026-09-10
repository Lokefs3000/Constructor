using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using CommunityToolkit.Diagnostics;
using Sigil.NonGeneric;

namespace PrimaryEditor.Inspector.Reflection
{
    public sealed class ValueSourceGenerator
    {
        private readonly AssemblyBuilder _assembly;
        private readonly ModuleBuilder _module;

        private Dictionary<object, InspectorValueSource> _valueSources;

        internal ValueSourceGenerator()
        {
            _assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("PrimaryEditor.Inspector.IL"), AssemblyBuilderAccess.RunAndCollect);
            _module = _assembly.DefineDynamicModule("PrimaryEditor.Inspector.IL");

            _valueSources = new Dictionary<object, InspectorValueSource>();
        }

        internal InspectorValueSource GetValueSource(FieldInfo field)
        {
            ref InspectorValueSource valueSource = ref CollectionsMarshal.GetValueRefOrAddDefault(_valueSources, field, out bool exists);
            if (!exists)
            {
                valueSource = CreateValueSourceForField(field);
            }

            return valueSource;
        }

        internal InspectorValueSource GetValueSource(PropertyInfo property)
        {
            ref InspectorValueSource valueSource = ref CollectionsMarshal.GetValueRefOrAddDefault(_valueSources, property, out bool exists);
            if (!exists)
            {
                valueSource = CreateValueSourceForProperty(property);
            }

            return valueSource;
        }

        private InspectorValueSource CreateValueSourceForField(FieldInfo field)
        {
            Delegate getDelegate;
            Delegate setDelegate;

            Type declaringType = field.DeclaringType!;
            Type targetType = field.FieldType;

            string fullFieldName = GetFullFieldName(field);

            // Generate getter delegate
            {
                if (declaringType.IsClass)
                {
                    Emit emit = Emit.NewDynamicMethod(targetType, [declaringType], $"GetObject_{fullFieldName}", _module);

                    emit.LoadArgument(0);
                    emit.LoadField(field);
                    emit.Return();

                    getDelegate = emit.CreateDelegate(typeof(GetValueAsObject<,>).MakeGenericType(targetType, declaringType));
                }
                else
                {
                    Emit emit = Emit.NewDynamicMethod(targetType, [declaringType.MakeByRefType()], $"GetValueType_{fullFieldName}", _module);

                    emit.LoadArgument(0);
                    emit.LoadField(field);
                    emit.Return();

                    getDelegate = emit.CreateDelegate(typeof(GetValueAsValueType<,>).MakeGenericType(targetType, declaringType));
                }
            }

            // Generate setter delegate
            {
                if (declaringType.IsClass)
                {
                    Emit emit = Emit.NewDynamicMethod(typeof(void), [declaringType, targetType], $"SetObject_{fullFieldName}", _module);

                    emit.LoadArgument(0);
                    emit.LoadArgument(1);
                    emit.StoreField(field);
                    emit.Return();

                    setDelegate = emit.CreateDelegate(typeof(SetValueAsObject<,>).MakeGenericType(targetType, declaringType));
                }
                else
                {
                    Emit emit = Emit.NewDynamicMethod(typeof(void), [declaringType.MakeByRefType(), targetType], $"SetValueType_{fullFieldName}", _module);

                    emit.LoadArgument(0);
                    emit.LoadArgument(1);
                    emit.StoreField(field);
                    emit.Return();

                    setDelegate = emit.CreateDelegate(typeof(SetValueAsValueType<,>).MakeGenericType(targetType, declaringType));
                }
            }

            return new InspectorValueSource(setDelegate, getDelegate, field.Name, fullFieldName);
        }

        private InspectorValueSource CreateValueSourceForProperty(PropertyInfo property)
        {
            Guard.IsNotNull(property.GetMethod);
            Guard.IsNotNull(property.SetMethod);

            Delegate getDelegate;
            Delegate setDelegate;

            Type declaringType = property.DeclaringType!;
            Type targetType = property.PropertyType;

            string fullPropertyName = GetFullPropertyName(property);

            // Generate getter delegate
            {
                if (declaringType.IsClass)
                {
                    Emit emit = Emit.NewDynamicMethod(targetType, [declaringType], $"GetObject_{fullPropertyName}", _module);

                    emit.LoadArgument(0);
                    emit.Call(property.GetMethod!);
                    emit.Return();

                    getDelegate = emit.CreateDelegate(typeof(GetValueAsObject<,>).MakeGenericType(targetType, declaringType));
                }
                else
                {
                    Emit emit = Emit.NewDynamicMethod(targetType, [declaringType.MakeByRefType()], $"GetValueType_{fullPropertyName}", _module);

                    emit.LoadArgument(0);
                    emit.Call(property.GetMethod!);
                    emit.Return();

                    getDelegate = emit.CreateDelegate(typeof(GetValueAsValueType<,>).MakeGenericType(targetType, declaringType));
                }
            }

            // Generate setter delegate
            {
                if (declaringType.IsClass)
                {
                    Emit emit = Emit.NewDynamicMethod(typeof(void), [declaringType, targetType], $"SetObject_{fullPropertyName}", _module);

                    emit.LoadArgument(0);
                    emit.LoadArgument(1);
                    emit.Call(property.SetMethod!);
                    emit.Return();

                    setDelegate = emit.CreateDelegate(typeof(SetValueAsObject<,>).MakeGenericType(targetType, declaringType));
                }
                else
                {
                    Emit emit = Emit.NewDynamicMethod(typeof(void), [declaringType.MakeByRefType(), targetType], $"SetValueType_{fullPropertyName}", _module);

                    emit.LoadArgument(0);
                    emit.LoadArgument(1);
                    emit.Call(property.SetMethod!);
                    emit.Return();

                    setDelegate = emit.CreateDelegate(typeof(SetValueAsValueType<,>).MakeGenericType(targetType, declaringType));
                }
            }

            return new InspectorValueSource(setDelegate, getDelegate, property.Name, fullPropertyName);
        }

        private static string GetFullFieldName(FieldInfo field) => $"{field.DeclaringType!.FullName}.{field.Name}";
        private static string GetFullPropertyName(PropertyInfo property) => $"{property.DeclaringType!.FullName}.{property.Name}";
    }
}
