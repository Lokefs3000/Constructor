using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Binding
{
    public sealed class DataBinding<T> : IDataBinding<T>
    {
        public DataBinding()
        {

        }

        public T Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
