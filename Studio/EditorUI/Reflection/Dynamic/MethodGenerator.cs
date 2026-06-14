using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters;
using System.Text;
using Sigil;
using Sigil.NonGeneric;
using TerraFX.Interop.Windows;

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

        internal PropertyMethods EmitMethods(PropertyInfo propertyInfo, FieldInfo? fieldInfo)
        {
            Delegate setDirect;
            if (fieldInfo != null)
            {
                setDirect = CreateSetFieldDirect(fieldInfo);
            }
            else
            {
                setDirect = CreateSetPropertyDirect(propertyInfo);
            }

            return new PropertyMethods(setDirect);
        }

        #region Property
        private Delegate CreateSetPropertyDirect(PropertyInfo propertyInfo)
        {
            Emit emit = Emit.NewDynamicMethod(typeof(SetPropertyDirect<>).GetMethod("Invoke")!.ReturnType, [typeof(object), propertyInfo.PropertyType.MakeByRefType()], $"s{propertyInfo.DeclaringType!.Name}.{propertyInfo.Name}", _module);
            MethodInfo setMethod = propertyInfo.SetMethod!;

            emit.LoadArgument(0);
            emit.CastClass(propertyInfo.DeclaringType!);

            emit.LoadArgument(1);
            if (propertyInfo.PropertyType.IsValueType)
                emit.LoadObject(propertyInfo.PropertyType);
            else
                emit.LoadIndirect(propertyInfo.PropertyType);

            emit.Call(setMethod);

            emit.Return();

            return emit.CreateDelegate(typeof(SetPropertyDirect<>).MakeGenericType(propertyInfo.PropertyType));
        }
        #endregion
        #region Field
        private Delegate CreateSetFieldDirect(FieldInfo fieldInfo)
        {
            Emit emit = Emit.NewDynamicMethod(typeof(SetFieldDirect<>).GetMethod("Invoke")!.ReturnType, [typeof(object), fieldInfo.FieldType.MakeByRefType()], $"s{fieldInfo.DeclaringType!.Name}.{fieldInfo.Name}", _module);

            emit.LoadArgument(0);
            emit.CastClass(fieldInfo.DeclaringType!);

            emit.LoadArgument(1);
            if (fieldInfo.FieldType.IsValueType)
                emit.LoadObject(fieldInfo.FieldType);
            else
                emit.LoadIndirect(fieldInfo.FieldType);

            emit.StoreField(fieldInfo);

            emit.Return();

            return emit.CreateDelegate(typeof(SetFieldDirect<>).MakeGenericType(fieldInfo.FieldType));
        }
        #endregion
    }
}
