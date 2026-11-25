using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Scenes.Json
{
    /// <summary>For internal use by the source generator.</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ComponentDeserializerAttribute : Attribute
    {
        private Type _target;

        public ComponentDeserializerAttribute(Type target)
        {
            _target = target;
        }

        public Type Target => _target;
    }
}
