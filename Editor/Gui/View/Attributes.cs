using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.View
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal sealed class ToolbarButtonAttribute(string Id) : Attribute
    {
        private readonly string _id = Id;

        public string Id => _id;
    }
}
