using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using PrimaryEditor.Inspector.Reflection;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Inspector.Setup
{
    public readonly record struct DescriptionValue(InspectorDescription? Description, Type ValueType, InspectorValueSource ValueSource, string Name)
    {
        public IInspectorValue Instantiate(Type parentType, IInspectorObject? parentObject, string name)
        {
            if (Description != null)
                return (IInspectorValue)Activator.CreateInstance(typeof(InspectorObject<,>).MakeGenericType(parentType, ValueType), BindingFlags.Instance | BindingFlags.NonPublic, null, [parentObject, ValueSource, name], null)!;
            else
                return (IInspectorValue)Activator.CreateInstance(typeof(InspectorValue<,>).MakeGenericType(parentType, ValueType), BindingFlags.Instance | BindingFlags.NonPublic, null, [parentObject, ValueSource, name], null)!;
        }

        public override string ToString() => ValueType.FullName!;
    }
}
