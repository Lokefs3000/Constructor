using System;
using System.Collections.Generic;
using System.Text;
using Editor.UI.Elements;
using Primary.Collections.ReadOnly;

namespace Editor.Gui.Inspector
{
    public record struct InspectorElementContext
    {
        private List<InspectorElement> _elements;

        private int _line;
        private InspectorViewDisplay _display;

        public InspectorElementContext()
        {
            _elements = new List<InspectorElement>();

            _line = 0;
            _display = InspectorViewDisplay.Default;
        }

        public int AddField<T>(float fill = -1.0f) where T : UITextField, new()
        {
            _elements.Add(new InspectorElement(typeof(T), fill, _line));
            return _elements.Count - 1;
        }

        public int AddButton<T>(float fill = -1.0f) where T : UIButton, new()
        {
            _elements.Add(new InspectorElement(typeof(T), fill, _line));
            return _elements.Count - 1;
        }

        public void AdvanceLine()
        {
            if (_elements.Count > 0 && _elements[^1].Line == _line)
                ++_line;
        }

        public InspectorViewDisplay Display { get => _display; set => _display = value; }

        internal readonly ROList<InspectorElement> Elements => _elements;
    }

    public enum InspectorViewDisplay : byte
    {
        Default = 0,

        AlwaysCollapsed,
        AlwaysExpanded
    }

    internal readonly record struct InspectorElement(Type ElementType, float Fill, int Line);
}
