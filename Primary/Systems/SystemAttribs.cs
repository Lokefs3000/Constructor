namespace Primary.Systems
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SystemRequirements : Attribute
    {
        private Type[] _requirements;

        public SystemRequirements(Type[] requirements)
        {
            _requirements = requirements;
        }

        public Type[] Requirements => _requirements;
    }
}
