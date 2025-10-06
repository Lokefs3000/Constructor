namespace Editor.Inspector.Entities
{
    internal abstract class ValueStorageBase
    {
        protected readonly ReflectionField _field;

        public ValueStorageBase(ReflectionField field)
        {
            _field = field;
        }

        public abstract object? Render(in string headerText, ref object @ref);

        public ReflectionField Field => _field;
    }
}
