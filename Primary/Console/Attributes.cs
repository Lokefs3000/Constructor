using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primary.Console
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class CommandClassNamespaceAttribute : Attribute
    {
        private readonly string _namespace;

        internal CommandClassNamespaceAttribute(string @namespace)
        {
            _namespace = @namespace;
        }

        internal string Namespace => _namespace;
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal class CommandAliasAttribute : Attribute
    {
        private readonly string _alias;

        internal CommandAliasAttribute(string alias)
        {
            _alias = alias;
        }

        internal string Alias => _alias;
    }
}
