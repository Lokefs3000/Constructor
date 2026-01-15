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

    internal interface ICVarModifier
    {

    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal class CVarRangeAttribute : Attribute, ICVarModifier
    {
        private readonly int _min;
        private readonly int _max;

        internal CVarRangeAttribute(int min, int max)
        {
            _min = min;
            _max = max;
        }

        internal int Min => _min;
        internal int Max => _max;
    }
}
