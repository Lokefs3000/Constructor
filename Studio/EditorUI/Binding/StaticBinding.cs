using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Binding
{
    public sealed class StaticBinding<T> : IDataBinding<T>
    {
        private T _value;

        public StaticBinding(T value)
        {
            _value = value;
        }

        public T Value { get => _value; set => _value = value; }
    }
}
