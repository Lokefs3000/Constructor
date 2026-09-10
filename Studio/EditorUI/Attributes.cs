using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace EditorUI
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ValueConverterAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class UIWidgetAttribute : Attribute
    {
    }
}
