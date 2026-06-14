using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Editor.Inspector
{
    internal sealed class InspectorConstructors
    {
        private Dictionary<TypeKey, InspectorObjectConstructor> _constructors;

        internal InspectorConstructors()
        {
            _constructors = new Dictionary<TypeKey, InspectorObjectConstructor>();
        }

        internal InspectorObjectConstructor GetConstructor(Type generic, Type value)
        {
            TypeKey typeKey = new TypeKey(generic, value);
            if (_constructors.TryGetValue(typeKey, out InspectorObjectConstructor? constructor))
                return constructor;

            Type specified = generic.MakeGenericType(value);
            ConstructorInfo? constructorInfo = specified.GetConstructor([typeof(InspectorManager), typeof(IInspectorBase), typeof(string), typeof(InspectorField)]);

            ConstructorInvoker invoker = ConstructorInvoker.Create(constructorInfo);
            constructor = (manager, parent, propertyName, field) => (IInspectorBase)invoker.Invoke(manager, parent, propertyName, field);

            _constructors.Add(typeKey, constructor);
            return constructor;
        }

        internal InspectorObjectConstructor GetInspectorObjectConstructor(Type type) => GetConstructor(typeof(InspectorObject<>), type);
        internal InspectorObjectConstructor GetInspectorValueConstructor(Type type) => GetConstructor(typeof(InspectorValue<>), type);

        private readonly record struct TypeKey(Type Generic, Type Value)
        {
            public override int GetHashCode() => HashCode.Combine(Generic, Value);
        }
    }

    internal delegate IInspectorBase InspectorObjectConstructor(InspectorManager manager, IInspectorBase? parent, string propertyName, InspectorField field);
}
