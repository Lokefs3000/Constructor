using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Console
{
    internal interface IGenericCVar
    {
        public Type VariableType { get; }

        public object GetValue();
        public void SetValue(object value);
    }

    internal struct CVar<T> : IGenericCVar where T : notnull
    {
        public T Value;

        public CVar(T value)
        {
            Value = value;
        }


        public object GetValue() => Value;

        public void SetValue(object value)
        {
            if (value is T t)
                Value = t;
        }

        public Type VariableType => typeof(T);
    }
}
