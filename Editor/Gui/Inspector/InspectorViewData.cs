using System;
using System.Collections.Generic;
using System.Text;
using Editor.UI.Elements;

namespace Editor.Gui.Inspector
{
    public readonly record struct InspectorViewData
    {
        private readonly UIElement[] _array;

        internal InspectorViewData(UIElement[] array)
        {
            _array = array;
        }

        public T GetElementAt<T>(int index) where T : UIElement
        {
            return (T)_array[index];
        }
    }
}
