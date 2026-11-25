using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.Gui.Inspector
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class InspectorTargetAttribute(Type type) : Attribute
    {
        private readonly Type _type = type;

        public Type Type => _type;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class InspectorHostTargetAttribute(Type type) : Attribute
    {
        private readonly Type _type = type;

        public Type Type => _type;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class InspectorViewTargetAttribute(Type type) : Attribute
    {
        private readonly Type _type = type;

        public Type Type => _type;
    }
}
