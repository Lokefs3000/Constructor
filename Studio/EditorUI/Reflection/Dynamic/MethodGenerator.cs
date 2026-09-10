using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters;
using System.Text;
using EditorUI.Common;
using EditorUI.Widgets;
using Sigil;
using Sigil.NonGeneric;
using TerraFX.Interop.Windows;
using Label = Sigil.Label;

namespace EditorUI.Reflection.Dynamic
{
    public sealed class MethodGenerator
    {
        private AssemblyBuilder _assembly;
        private ModuleBuilder _module;

        internal MethodGenerator()
        {
            _assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("EditorUI.IL"), AssemblyBuilderAccess.Run);
            _module = _assembly.DefineDynamicModule("EditorUI.IL");
        }

        internal PropertyMethods EmitMethods(Type sourceType, ReadOnlySpan<MemberInfo> memberInfos)
        {
            Delegate? setDirect = null;
            SetIndirect? setIndirect = null;

            if (!memberInfos.IsEmpty)
            {
                setDirect = CreateSetDirect(sourceType, memberInfos);
                setIndirect = CreateSetIndirect(sourceType, memberInfos);
            }

            return new PropertyMethods(setDirect, setIndirect);
        }

        private Delegate CreateSetDirect(Type sourceType, ReadOnlySpan<MemberInfo> memberInfos)
        {
            MemberInfo lastMemberInfo = memberInfos[^1];
            Type lastMemberType = GetMemberInfoType(lastMemberInfo);

            Emit emit = Emit.NewDynamicMethod(typeof(SetDirect<>).GetMethod("Invoke")!.ReturnType, [typeof(object), lastMemberType.MakeByRefType()], $"s{sourceType.DeclaringType!.Name}.{memberInfos.Length}-{lastMemberInfo.Name}", _module);

            emit.LoadArgument(0);
            for (int i = 0; i < memberInfos.Length - 1; ++i)
            {
                MemberInfo memberInfo = memberInfos[i];
                Type memberType = GetMemberInfoType(memberInfo);

                if (memberInfo is PropertyInfo propertyInfo)
                {
                    throw new InvalidOperationException("Properties must always be set last");
                }
                else if (memberInfo is FieldInfo fieldInfo)
                {
                    if (fieldInfo.DeclaringType!.IsClass)
                        emit.CastClass(fieldInfo.DeclaringType!);

                    if (fieldInfo.FieldType.IsValueType)
                        emit.LoadFieldAddress(fieldInfo);
                    else
                        emit.LoadField(fieldInfo);
                }
                else
                {
                    throw new UnreachableException();
                }
            }

            {
                if (lastMemberInfo is PropertyInfo propertyInfo)
                {
                    MethodInfo setMethod = propertyInfo.SetMethod!;

                    if (setMethod.DeclaringType!.IsClass)
                        emit.CastClass(setMethod.DeclaringType!);

                    emit.LoadArgument(1);
                    emit.CallVirtual(setMethod);
                }
                else if (lastMemberInfo is FieldInfo fieldInfo)
                {
                    if (fieldInfo.DeclaringType!.IsClass)
                        emit.CastClass(fieldInfo.DeclaringType!);

                    emit.LoadArgument(1);
                    emit.StoreField(fieldInfo);
                }
                else
                {
                    throw new UnreachableException();
                }
            }

            emit.Return();

            return emit.CreateDelegate(typeof(SetDirect<>).MakeGenericType(lastMemberType));
        }

        private SetIndirect CreateSetIndirect(Type sourceType, ReadOnlySpan<MemberInfo> memberInfos)
        {
            MemberInfo lastMemberInfo = memberInfos[^1];
            Type lastMemberType = GetMemberInfoType(lastMemberInfo);

            Emit emit = Emit.NewDynamicMethod(typeof(SetDirect<>).GetMethod("Invoke")!.ReturnType, [typeof(object), lastMemberType.MakeByRefType()], $"s{sourceType.DeclaringType!.Name}.{memberInfos.Length}-{lastMemberInfo.Name}", _module);

            Label isNullLabel = emit.DefineLabel();
            Label setLabel = emit.DefineLabel();

            emit.LoadArgument(0);
            for (int i = 0; i < memberInfos.Length - 1; ++i)
            {
                MemberInfo memberInfo = memberInfos[i];
                Type memberType = GetMemberInfoType(memberInfo);

                if (memberInfo is PropertyInfo propertyInfo)
                {
                    throw new InvalidOperationException("Properties must always be set last");
                }
                else if (memberInfo is FieldInfo fieldInfo)
                {
                    if (fieldInfo.DeclaringType!.IsClass)
                        emit.CastClass(fieldInfo.DeclaringType!);

                    if (fieldInfo.FieldType.IsValueType)
                        emit.LoadFieldAddress(fieldInfo);
                    else
                        emit.LoadField(fieldInfo);
                }
                else
                {
                    throw new UnreachableException();
                }
            }

            {
                if (lastMemberInfo is PropertyInfo propertyInfo)
                {
                    MethodInfo setMethod = propertyInfo.SetMethod!;

                    if (setMethod.DeclaringType!.IsClass)
                        emit.CastClass(setMethod.DeclaringType!);

                    PushObjectUntoStack();

                    emit.MarkLabel(setLabel);
                    emit.CallVirtual(setMethod);
                }
                else if (lastMemberInfo is FieldInfo fieldInfo)
                {
                    if (fieldInfo.DeclaringType!.IsClass)
                        emit.CastClass(fieldInfo.DeclaringType!);

                    PushObjectUntoStack();

                    emit.MarkLabel(setLabel);
                    emit.StoreField(fieldInfo);
                }
                else
                {
                    throw new UnreachableException();
                }

                void PushObjectUntoStack()
                {
                    emit.LoadArgument(1);
                    if (lastMemberType.IsClass)
                    {
                        emit.CastClass(lastMemberType);
                    }
                    else
                    {
                        // if (lastMemberType.IsGenericType && !lastMemberType.IsGenericTypeDefinition)
                        // {
                        //     Type genericType = lastMemberType.GetGenericTypeDefinition();
                        //     if (genericType == typeof(Nullable<>))
                        //     {
                        //         emit.Call(lastMemberType.GetProperty("HasValue")!.GetMethod!);
                        //         emit.BranchIfTrue(isNullLabel);
                        // 
                        //         // Not null
                        //         emit.LoadArgument(1);
                        //         emit.Unbox(lastMemberType);
                        //         emit.Call(typeof(Nullable<>).MakeGenericType(lastMemberType).GetConstructor([lastMemberType]));
                        //         emit.Branch(setLabel);
                        // 
                        //         // Is null
                        //         emit.MarkLabel(isNullLabel);
                        //         emit.Call(typeof(Nullable<>).MakeGenericType(lastMemberType).GetConstructor(Type.EmptyTypes));
                        //     }
                        //     else if (genericType == typeof(LayoutValue<>))
                        //     {
                        //         emit.Call(lastMemberType.GetProperty("HasValue")!.GetMethod!);
                        //         emit.BranchIfTrue(isNullLabel);
                        // 
                        //         // Not null
                        //         emit.LoadArgument(1);
                        //         emit.Unbox(lastMemberType);
                        //         emit.Call(typeof(LayoutValue<>).MakeGenericType(lastMemberType).GetConstructor([lastMemberType]));
                        //         emit.Branch(setLabel);
                        // 
                        //         // Is null
                        //         emit.MarkLabel(isNullLabel);
                        //         emit.Call(typeof(LayoutValue<>).MakeGenericType(lastMemberType).GetConstructor(Type.EmptyTypes));
                        //     }
                        // }
                    }
                }
            }

            emit.Return();

            return emit.CreateDelegate<SetIndirect>();
        }

        private static Type GetMemberInfoType(MemberInfo memberInfo) => memberInfo is FieldInfo ? ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType;

        private static bool IsTypeNullable(Type type)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                Type genericType = type.GetGenericTypeDefinition();
                return genericType == typeof(Nullable<>) || genericType == typeof(LayoutValue<>);
            }

            return false;
        }
    }
}
