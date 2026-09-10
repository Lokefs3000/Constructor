using System;
using System.Collections.Generic;
using System.Text;
using PrimaryEditor.Inspector.Values;

namespace PrimaryEditor.Inspector.Reflection
{
    public readonly record struct InspectorValueSource(Delegate SetDelegate, Delegate GetDelegate, string LocalName, string TargetName)
    {
        public void SetValueFromValueType<T, TValueType>(ref TValueType valueType, T? value)
        {
            ((SetValueAsValueType<T, TValueType>)SetDelegate)(ref valueType, value);
        }

        public void SetValueFromObject<T, TObject>(TObject obj, T? value)
        {
            ((SetValueAsObject<T, TObject>)SetDelegate)(obj, value);
        }

        public T? GetValueFromValueType<T, TValueType>(ref TValueType valueType)
        {
            return ((GetValueAsValueType<T, TValueType>)GetDelegate)(ref valueType);
        }

        public T? GetValueFromObject<T, TObject>(TObject obj)
        {
            return obj is null ? default : ((GetValueAsObject<T, TObject>)GetDelegate)(obj);
        }
    }

    public delegate TReturn? GetValueAsObject<TReturn, TObject>(TObject obj);
    public delegate void SetValueAsObject<TValue, TObject>(TObject obj, TValue? value);

    public delegate TReturn? GetValueAsValueType<TReturn, TValueType>(ref TValueType valueType);
    public delegate void SetValueAsValueType<TValue, TValueType>(ref TValueType valueType, TValue? value);
}
