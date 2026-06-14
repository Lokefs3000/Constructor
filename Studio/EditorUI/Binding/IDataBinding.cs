using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Binding
{
    public interface IDataBinding<T>
    {
        public T Value { get; set; }
    }
}
