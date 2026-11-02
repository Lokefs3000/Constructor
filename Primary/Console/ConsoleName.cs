using System;
using System.Collections.Generic;
using System.Text;

namespace Primary.Console
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class ConsoleNameAttribute : Attribute
    {
        private readonly string _fullName;
        private readonly bool _inheritParent;

        public ConsoleNameAttribute(string fullName, bool inheritParent = true)
        {
            _fullName = fullName;
            _inheritParent = inheritParent;
        }

        public string FullName => _fullName;
        public bool InheritParent => _inheritParent;
    }
}
