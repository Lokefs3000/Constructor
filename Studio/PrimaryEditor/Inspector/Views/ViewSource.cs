using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace PrimaryEditor.Inspector.Views
{
    public readonly record struct ViewSource(Type Type, string Name, object Opaque)
    {
        public FieldInfo? Field => Opaque as FieldInfo;
        public PropertyInfo? Property => Opaque as PropertyInfo;
    }
}
