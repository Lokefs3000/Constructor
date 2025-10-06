namespace Primary.Components
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class ComponentRequirementsAttribute : Attribute
    {
        private Type[] _required;

        public ComponentRequirementsAttribute(params Type[] Required)
        {
            _required = Required;
        }

        public Type[] RequiredTypes => _required;
    }

    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class ComponentConnectionsAttribute : Attribute
    {
        private Type[] _connected;

        public ComponentConnectionsAttribute(params Type[] Connected)
        {
            _connected = Connected;
        }

        public Type[] ConnectedTypes => _connected;
    }

    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class ComponentUsageAttribute : Attribute
    {
        private bool _canBeAdded;
        private bool _hasSelfRef;

        public ComponentUsageAttribute(bool CanBeAdded = true, bool HasSelfReference = false)
        {
            _canBeAdded = CanBeAdded;
            _hasSelfRef = HasSelfReference;
        }

        public bool CanBeAdded => _canBeAdded;
        public bool HasSelfReference => _hasSelfRef;
    }

    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class DontSerializeComponentAttribute : Attribute
    {

    }
}
