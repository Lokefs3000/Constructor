namespace Editor.Interaction.Logic
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class SelectionLogicAttribute : Attribute
    {
        private readonly Type _logic;

        public SelectionLogicAttribute(Type logic)
        {
            _logic = logic;
        }

        public Type Logic => _logic;
    }
}
