using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using Primary.Collections;
using Sigil.NonGeneric;

namespace PrimaryEditor.Inspector.Reflection
{
    public sealed class ValueSourceGenerator
    {
        private Dictionary<Assembly, DynamicAssembly> _dynamicAssemblies;
        private Dictionary<object, InspectorValueSource> _valueSources;

        internal ValueSourceGenerator()
        {
            _dynamicAssemblies = new Dictionary<Assembly, DynamicAssembly>();
            _valueSources = new Dictionary<object, InspectorValueSource>();
        }

        internal void RemoveAllSourcesReferencing(Assembly assembly)
        {
            using RentedList<object> keysToRemove = new RentedList<object>();
            foreach (var (key, valueSource) in _valueSources)
            {
                if (key.GetType().Assembly == assembly ||
                    valueSource.SetDelegate.Method.ReturnType.Assembly == assembly ||
                    valueSource.GetDelegate.Method.ReturnType.Assembly == assembly)
                {
                    keysToRemove.Add(key);
                }
            }

            if (!keysToRemove.IsEmpty)
            {
                foreach (object key in keysToRemove)
                {
                    _valueSources.Remove(key);
                }
            }

            _dynamicAssemblies.Remove(assembly);
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
            DynamicAssembly assembly = GetDynamicAssembly(field.DeclaringType!.Assembly);

            Delegate getDelegate;
            Delegate setDelegate;

            Type declaringType = field.DeclaringType!;
            Type targetType = field.FieldType;

            string fullFieldName = GetFullFieldName(field);

            // Generate getter delegate
            {
                if (declaringType.IsClass)
                {
                    Emit emit = Emit.NewDynamicMethod(targetType, [declaringType], $"GetObject_{fullFieldName}", assembly.Module);

                    emit.LoadArgument(0);
                    emit.LoadField(field);
                    emit.Return();

                    getDelegate = emit.CreateDelegate(typeof(GetValueAsObject<,>).MakeGenericType(targetType, declaringType));
                }
                else
                {
                    Emit emit = Emit.NewDynamicMethod(targetType, [declaringType.MakeByRefType()], $"GetValueType_{fullFieldName}", assembly.Module);

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
                    Emit emit = Emit.NewDynamicMethod(typeof(void), [declaringType, targetType], $"SetObject_{fullFieldName}", assembly.Module);

                    emit.LoadArgument(0);
                    emit.LoadArgument(1);
                    emit.StoreField(field);
                    emit.Return();

                    setDelegate = emit.CreateDelegate(typeof(SetValueAsObject<,>).MakeGenericType(targetType, declaringType));
                }
                else
                {
                    Emit emit = Emit.NewDynamicMethod(typeof(void), [declaringType.MakeByRefType(), targetType], $"SetValueType_{fullFieldName}", assembly.Module);

                    emit.LoadArgument(0);
                    emit.LoadArgument(1);
                    emit.StoreField(field);
                    emit.Return();

                    setDelegate = emit.CreateDelegate(typeof(SetValueAsValueType<,>).MakeGenericType(targetType, declaringType));
                }
            }

            return new InspectorValueSource(setDelegate, getDelegate, declaringType, field.Name, fullFieldName);
        }

        private InspectorValueSource CreateValueSourceForProperty(PropertyInfo property)
        {
            Guard.IsNotNull(property.GetMethod);
            Guard.IsNotNull(property.SetMethod);

            DynamicAssembly assembly = GetDynamicAssembly(property.DeclaringType!.Assembly);

            Delegate getDelegate;
            Delegate setDelegate;

            Type declaringType = property.DeclaringType!;
            Type targetType = property.PropertyType;

            string fullPropertyName = GetFullPropertyName(property);

            // Generate getter delegate
            {
                if (declaringType.IsClass)
                {
                    Emit emit = Emit.NewDynamicMethod(targetType, [declaringType], $"GetObject_{fullPropertyName}", assembly.Module);

                    emit.LoadArgument(0);
                    emit.Call(property.GetMethod!);
                    emit.Return();

                    getDelegate = emit.CreateDelegate(typeof(GetValueAsObject<,>).MakeGenericType(targetType, declaringType));
                }
                else
                {
                    Emit emit = Emit.NewDynamicMethod(targetType, [declaringType.MakeByRefType()], $"GetValueType_{fullPropertyName}", assembly.Module);

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
                    Emit emit = Emit.NewDynamicMethod(typeof(void), [declaringType, targetType], $"SetObject_{fullPropertyName}", assembly.Module);

                    emit.LoadArgument(0);
                    emit.LoadArgument(1);
                    emit.Call(property.SetMethod!);
                    emit.Return();

                    setDelegate = emit.CreateDelegate(typeof(SetValueAsObject<,>).MakeGenericType(targetType, declaringType));
                }
                else
                {
                    Emit emit = Emit.NewDynamicMethod(typeof(void), [declaringType.MakeByRefType(), targetType], $"SetValueType_{fullPropertyName}", assembly.Module);

                    emit.LoadArgument(0);
                    emit.LoadArgument(1);
                    emit.Call(property.SetMethod!);
                    emit.Return();

                    setDelegate = emit.CreateDelegate(typeof(SetValueAsValueType<,>).MakeGenericType(targetType, declaringType));
                }
            }

            return new InspectorValueSource(setDelegate, getDelegate, declaringType, property.Name, fullPropertyName);
        }

        private DynamicAssembly GetDynamicAssembly(Assembly assembly)
        {
            ref DynamicAssembly value = ref CollectionsMarshal.GetValueRefOrAddDefault(_dynamicAssemblies, assembly, out bool exists);
            if (!exists)
            {
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"InspectorIL.{(uint)assembly.GetHashCode()}"), AssemblyBuilderAccess.RunAndCollect);
                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Main");

                value = new DynamicAssembly(assemblyBuilder, moduleBuilder);
            }

            return value;
        }

        private static string GetFullFieldName(FieldInfo field) => $"{field.DeclaringType!.FullName}.{field.Name}";
        private static string GetFullPropertyName(PropertyInfo property) => $"{property.DeclaringType!.FullName}.{property.Name}";

        private readonly record struct DynamicAssembly(AssemblyBuilder Assembly, ModuleBuilder Module);
    }
}
