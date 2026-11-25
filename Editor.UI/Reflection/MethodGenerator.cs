using CommunityToolkit.Diagnostics;
using Editor.UI.Serialization.Values;
using Sigil;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.UI.Reflection
{
    public sealed class MethodGenerator
    {
        private Dictionary<FieldInfo, Delegate> _fieldDelegates;

        internal MethodGenerator()
        {
            _fieldDelegates = new Dictionary<FieldInfo, Delegate>();
        }

        public Action<object, T> GetSetterDelegate<T>(FieldInfo field) => Unsafe.As<Action<object, T>>(GetSetterDelegate(field));

        public object GetSetterDelegate(FieldInfo field)
        {
            if (_fieldDelegates.TryGetValue(field, out Delegate? value))
                return value;

            Type declaring = field.DeclaringType!;
            Type type = field.FieldType;

            DynamicMethod method = new DynamicMethod($"{declaring.FullName}.{field.Name}_dynSet", null, [typeof(object), type], true);
            ILGenerator il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);

            Type delegateType = typeof(Action<,>).MakeGenericType([typeof(object), type]);

            Delegate action = method.CreateDelegate(delegateType);
            _fieldDelegates.Add(field, action);

            return action;
        }

        internal static DeserializeFieldSet CreateDeserializeFieldSet(Type t, Type serializer)
        {
            Type genericInvokeType = typeof(Action<,>).MakeGenericType([typeof(object), t]);

            Emit<DeserializeFieldSet> emitter = Emit<DeserializeFieldSet>.NewDynamicMethod($"{t.Name}_DeserializeFieldSet");
            {
                /*
                if (TSerializer.Deserialize<T>(value, out T? temp))
                {
                    result = temp;
                    return true;
                }
                temp = null;
                return false; 
                */

                Local result = emitter.DeclareLocal(t, "result");
                Sigil.Label label = emitter.DefineLabel("retFalse");

                emitter.LoadArgument(0);
                emitter.LoadLocalAddress(result);
                emitter.Call(serializer.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public)!);

                emitter.BranchIfFalse(label);
                {
                    emitter.LoadArgument(1);
                    emitter.CastClass(genericInvokeType);
                    emitter.LoadArgument(2);
                    emitter.LoadLocal(result);

                    emitter.Call(genericInvokeType.GetMethod("Invoke")!);

                    emitter.LoadConstant(true);
                    emitter.Return();
                }
                emitter.MarkLabel(label);
                {
                    emitter.LoadConstant(false);
                    emitter.Return();
                }
            }

            return emitter.CreateDelegate();
        }

        internal static DeserializeFieldSet CreateDeserializeFieldSetEnum(Type t)
        {
            MethodInfo invokeMethod = typeof(Enum).GetMethods(BindingFlags.Static | BindingFlags.Public).First((x) =>
                {
                    if (!x.ContainsGenericParameters)
                        return false;

                    ParameterInfo[] parameters = x.GetParameters();
                    if (parameters.Length != 2)
                        return false;

                    bool r0 = parameters[0].ParameterType == typeof(string);
                    bool r1 = parameters[1].ParameterType.ContainsGenericParameters && parameters[1].ParameterType.IsByRef;

                    return r0 && r1;
                }).MakeGenericMethod([t]);

            Type genericInvokeType = typeof(Action<,>).MakeGenericType([typeof(object), t]);

            Emit<DeserializeFieldSet> emitter = Emit<DeserializeFieldSet>.NewDynamicMethod($"{t.Name}_DeserializeFieldSet");
            {
                /*
                if (TSerializer.Deserialize<T>(value, out T? temp))
                {
                    result = temp;
                    return true;
                }
                temp = null;
                return false; 
                */

                Local result = emitter.DeclareLocal(t, "result");
                Sigil.Label label = emitter.DefineLabel("retFalse");

                emitter.LoadArgument(0);
                emitter.LoadLocalAddress(result);
                emitter.Call(invokeMethod);

                emitter.BranchIfFalse(label);
                {
                    emitter.LoadArgument(1);
                    emitter.CastClass(genericInvokeType);
                    emitter.LoadArgument(2);
                    emitter.LoadLocal(result);

                    emitter.Call(genericInvokeType.GetMethod("Invoke")!);

                    emitter.LoadConstant(true);
                    emitter.Return();
                }
                emitter.MarkLabel(label);
                {
                    emitter.LoadConstant(false);
                    emitter.Return();
                }
            }

            return emitter.CreateDelegate();
        }

        internal static DeserializeBoxed CreateDeserializeBoxed(Type t, Type serializer)
        {
            Emit<DeserializeBoxed> emitter = Emit<DeserializeBoxed>.NewDynamicMethod($"{t.Name}_DeserializeBoxed");
            {
                /*
                if (TSerializer.Deserialize<T>(value, out T? temp))
                {
                    result = temp;
                    return true;
                }
                //temp:SkipInit
                return false; 
                */

                Local temp = emitter.DeclareLocal(t, "temp");
                Sigil.Label label = emitter.DefineLabel("retFalse");

                emitter.LoadArgument(0);
                emitter.LoadLocalAddress(temp);
                emitter.Call(serializer.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public)!);

                emitter.BranchIfFalse(label);
                {
                    emitter.LoadArgument(1);
                    emitter.LoadLocal(temp);
                    if (t.IsValueType)
                        emitter.Box(t);
                    emitter.StoreIndirect(typeof(object));

                    emitter.LoadConstant(true);
                    emitter.Return();
                }
                emitter.MarkLabel(label);
                {
                    emitter.LoadConstant(false);
                    emitter.Return();
                }
            }

            return emitter.CreateDelegate();
        }
    }

    internal delegate bool DeserializeFieldSet(string value, object fieldSetter, object fieldTarget);
    internal delegate bool DeserializeBoxed(string value, out object? result);
}
