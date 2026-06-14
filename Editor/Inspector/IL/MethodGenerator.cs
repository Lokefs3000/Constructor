using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using System.Text;
using CommunityToolkit.Diagnostics;
using Sigil;
using TerraFX.Interop.Windows;

namespace Editor.Inspector.IL
{
    internal sealed class MethodGenerator
    {
        private Dictionary<MethodKey, MethodTuple> _tuples;

        private AssemblyBuilder _assembly;
        private ModuleBuilder? _primaryModule;

        private int _generatedIlSize;

        internal MethodGenerator()
        {
            _tuples = new Dictionary<MethodKey, MethodTuple>();

            _assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("InspectorDynamic"), AssemblyBuilderAccess.Run);
            _primaryModule = _assembly.DefineDynamicModule("Main");

            _generatedIlSize = 0;
        }

        private GetInspectedByRefValue<T>? CreateByRefGetter<T>(InspectorField field)
        {
            if (!field.DeclaringType!.IsValueType)
                return null;

            Emit<GetInspectedByRefValue<T>> emit = Emit<GetInspectedByRefValue<T>>.NewDynamicMethod($"ByRefGetter<{typeof(T)}>", _primaryModule, false);

            // local arguments
            //     0: object fieldOrProperty
            //     1: ref GenericRefValue target

            Local retLocal = emit.DeclareLocal<T>();
            Local valLocal = emit.DeclareLocal(field.FieldType);

            emit.LoadArgument(1);

            if (field.IsPropertyType)
            {
                PropertyInfo propertyInfo = field.Property!;
                MethodInfo? getMethod = propertyInfo.GetGetMethod();

                Guard.IsNotNull(getMethod);

                emit.CallVirtual(getMethod);
            }
            else
            {
                emit.LoadField(field.Field!);
            }

            if (field.SourceIndex != -1)
            {
                emit.StoreLocal(valLocal);

                if (field.FieldType.IsArray)
                {
                    emit.LoadLocal(valLocal);
                    emit.LoadConstant(field.SourceIndex);
                    emit.LoadElement<T>();
                }
                else
                {
                    MethodInfo? getMethod = field.FieldType.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public);
                    Guard.IsNotNull(getMethod);

                    if (field.FieldType.IsByRefLike)
                        emit.LoadLocalAddress(valLocal);
                    else
                        emit.LoadLocal(valLocal);

                    emit.LoadConstant(field.SourceIndex);
                    emit.Call(getMethod);

                    emit.LoadObject(typeof(T));
                }
            }

            emit.StoreLocal(retLocal);
            emit.LoadLocal(retLocal);

            emit.Return();

            _generatedIlSize += emit.ILOffset();
            return emit.CreateDelegate();
        }

        private SetInspectedByRefValue<T>? CreateByRefSetter<T>(InspectorField field)
        {
            if (!field.DeclaringType!.IsValueType)
                return null;

            Emit<SetInspectedByRefValue<T>> emit = Emit<SetInspectedByRefValue<T>>.NewDynamicMethod($"ByRefSetter<{typeof(T)}>", _primaryModule, false);

            // local arguments
            //     0: object fieldOrProperty
            //     1: ref GenericRefValue target
            //     2: T value

            Local storeLocal = emit.DeclareLocal<T>();

            emit.LoadArgument(1);

            if (field.IsPropertyType)
            {
                PropertyInfo propertyInfo = field.Property!;
                MethodInfo? setMethod = propertyInfo.GetSetMethod();
                if (setMethod != null)
                {
                    emit.LoadArgument(2);
                    emit.Call(setMethod);

                    goto EndDelegate;
                }

                MethodInfo? getMethod = propertyInfo.GetGetMethod();

                Guard.IsNotNull(getMethod);
                Guard.IsTrue(getMethod.ReturnType.IsByRefLike);

                emit.Call(getMethod);
            }
            else if (field.FieldType == typeof(T) && !field.FieldType.IsArray)
            {
                emit.LoadArgument(2);
                emit.StoreField(field.Field!);
                goto EndDelegate;
            }
            else
            {
                emit.LoadField(field.Field!);
            }

            if (field.SourceIndex != -1)
            {
                if (field.FieldType.IsArray)
                {
                    emit.StoreElement(field.GetCanonicalType());
                    goto EndDelegate;
                }

                Local spanLocal = emit.DeclareLocal(field.FieldType);

                MethodInfo? getMethod = field.FieldType.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public);
                Guard.IsNotNull(getMethod);

                emit.StoreLocal(spanLocal);

                if (field.FieldType.IsByRefLike)
                    emit.LoadLocalAddress(spanLocal);
                else
                    emit.LoadLocal(spanLocal);

                emit.LoadConstant(field.SourceIndex);
                emit.Call(getMethod);
            }

            emit.StoreLocal(storeLocal);

            if (typeof(T).IsByRefLike)
            {
                emit.LoadLocalAddress(storeLocal);
                emit.LoadArgument(2);

                emit.StoreIndirect(typeof(T));
            }
            else
            {
                emit.LoadLocal(storeLocal);
                emit.LoadArgument(2);

                emit.StoreObject(typeof(T));
            }

        EndDelegate:
            emit.Return();

            _generatedIlSize += emit.ILOffset();
            return emit.CreateDelegate();
        }

        private GetInspectedObjectValue<T>? CreateObjectGetter<T>(InspectorField field)
        {
            if (!field.DeclaringType!.IsClass)
                return null;

            Emit<GetInspectedObjectValue<T>> emit = Emit<GetInspectedObjectValue<T>>.NewDynamicMethod($"ObjectGetter<{typeof(T)}>", _primaryModule, false);

            // local arguments
            //     0: object fieldOrProperty
            //     1: object target

            Local retLocal = emit.DeclareLocal<T>();
            Local valLocal = emit.DeclareLocal(field.FieldType);

            emit.LoadArgument(1);
            emit.CastClass(field.DeclaringType!);

            if (field.IsPropertyType)
            {
                PropertyInfo propertyInfo = field.Property!;
                MethodInfo? getMethod = propertyInfo.GetGetMethod();

                Guard.IsNotNull(getMethod);

                emit.CallVirtual(getMethod);
            }
            else
            {
                emit.LoadField(field.Field!);
            }

            if (field.SourceIndex != -1)
            {
                emit.StoreLocal(valLocal);

                if (field.FieldType.IsArray)
                {
                    emit.LoadLocal(valLocal);
                    emit.LoadConstant(field.SourceIndex);
                    emit.LoadElement<T>();
                }
                else
                {
                    MethodInfo? getMethod = field.FieldType.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public);
                    Guard.IsNotNull(getMethod);

                    if (field.FieldType.IsByRefLike)
                        emit.LoadLocalAddress(valLocal);
                    else
                        emit.LoadLocal(valLocal);

                    emit.LoadConstant(field.SourceIndex);
                    emit.Call(getMethod);

                    emit.LoadObject(typeof(T));
                }
            }

            emit.StoreLocal(retLocal);
            emit.LoadLocal(retLocal);

            emit.Return();

            _generatedIlSize += emit.ILOffset();
            return emit.CreateDelegate();
        }

        private SetInspectedObjectValue<T>? CreateObjectSetter<T>(InspectorField field)
        {
            if (!field.DeclaringType!.IsClass)
                return null;

            Emit<SetInspectedObjectValue<T>> emit = Emit<SetInspectedObjectValue<T>>.NewDynamicMethod($"ObjectSetter<{typeof(T)}>", _primaryModule, false);

            // local arguments
            //     0: object fieldOrProperty
            //     1: object target
            //     2: T value

            Local storeLocal = emit.DeclareLocal<T>();

            emit.LoadArgument(1);
            emit.CastClass(field.DeclaringType!);

            if (field.IsPropertyType)
            {
                PropertyInfo propertyInfo = field.Property!;
                MethodInfo? setMethod = propertyInfo.GetSetMethod();
                if (setMethod != null)
                {
                    emit.LoadArgument(2);
                    emit.Call(setMethod);

                    goto EndDelegate;
                }

                MethodInfo? getMethod = propertyInfo.GetGetMethod();

                Guard.IsNotNull(getMethod);
                Guard.IsTrue(getMethod.ReturnType.IsByRefLike);

                emit.Call(getMethod);
            }
            else if (field.FieldType == typeof(T) && !field.FieldType.IsArray)
            {
                emit.LoadArgument(2);
                emit.StoreField(field.Field!);
                goto EndDelegate;
            }
            else
            {
                emit.LoadField(field.Field!);
            }

            if (field.SourceIndex != -1)
            {
                if (field.FieldType.IsArray)
                {
                    emit.StoreElement(field.GetCanonicalType());
                    goto EndDelegate;
                }

                Local spanLocal = emit.DeclareLocal(field.FieldType);

                MethodInfo? getMethod = field.FieldType.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public);
                Guard.IsNotNull(getMethod);

                emit.StoreLocal(spanLocal);

                if (field.FieldType.IsByRefLike)
                    emit.LoadLocalAddress(spanLocal);
                else
                    emit.LoadLocal(spanLocal);

                emit.LoadConstant(field.SourceIndex);
                emit.Call(getMethod);
            }

            emit.StoreLocal(storeLocal);

            if (typeof(T).IsByRefLike)
            {
                emit.LoadLocalAddress(storeLocal);
                emit.LoadArgument(2);

                emit.StoreIndirect(typeof(T));
            }
            else
            {
                emit.LoadLocal(storeLocal);
                emit.LoadArgument(2);

                emit.StoreObject(typeof(T));
            }

        EndDelegate:
            emit.Return();

            _generatedIlSize += emit.ILOffset();
            return emit.CreateDelegate();
        }

        internal MethodTuple GetMethods<T>(InspectorField field, string propertyName)
        {
            ref MethodTuple tuple = ref CollectionsMarshal.GetValueRefOrAddDefault(_tuples, new MethodKey(typeof(T), propertyName), out bool exists);
            if (!exists)
                tuple = new MethodTuple(CreateByRefGetter<T>(field), CreateByRefSetter<T>(field), CreateObjectGetter<T>(field), CreateObjectSetter<T>(field));

            return tuple;
        }

        private static FieldInfo s_getObjectRefValueField = typeof(GenericRefObject).GetField("Value")!;
    }

    internal readonly record struct MethodKey(Type BaseType, string Expression);
    internal readonly record struct MethodTuple(object? ByRefGetter, object? ByRefSetter, object? ObjectGetter, object? ObjectSetter)
    {
        public GetInspectedByRefValue<T>? GetByRefGetter<T>() => (GetInspectedByRefValue<T>?)ByRefGetter;
        public SetInspectedByRefValue<T>? GetByRefSetter<T>() => (SetInspectedByRefValue<T>?)ByRefSetter;

        public GetInspectedObjectValue<T>? GetObjectGetter<T>() => (GetInspectedObjectValue<T>?)ObjectGetter;
        public SetInspectedObjectValue<T>? GetObjectSetter<T>() => (SetInspectedObjectValue<T>?)ObjectSetter;
    }
}
