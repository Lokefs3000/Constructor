using System;
using System.Collections.Generic;
using System.Text;

namespace EditorUI.Binding
{
    public interface ICollectionBinding<T>
    {
        public T GetItemAt(int index);

        public int Count { get; }
    }
}
