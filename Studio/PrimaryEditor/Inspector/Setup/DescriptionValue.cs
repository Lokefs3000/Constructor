using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using PrimaryEditor.Inspector.Reflection;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Inspector.Setup
{
    public readonly record struct DescriptionValue(InspectorDescription? Description, Type ValueType, InspectorValueSource ValueSource)
    {
        public IInspectorValue Instantiate(Type parentType, IInspectorObject? parentObject)
        {
            if (Description != null)
                return (IInspectorValue)Activator.CreateInstance(typeof(InspectorObject<,>).MakeGenericType(parentType, ValueType), BindingFlags.Instance | BindingFlags.NonPublic, null, [parentObject, ValueSource], null)!;
            else
                return (IInspectorValue)Activator.CreateInstance(typeof(InspectorValue<,>).MakeGenericType(parentType, ValueType), BindingFlags.Instance | BindingFlags.NonPublic, null, [parentObject, ValueSource], null)!;
        }

        public override string ToString() => ValueType.FullName!;
    }
}
