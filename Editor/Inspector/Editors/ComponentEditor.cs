using Primary.Scenes;

namespace Editor.Inspector.Editors
{
    internal abstract class ComponentEditor
    {
        public abstract void SetupInspectorFields(SceneEntity entity, Type type);
        public abstract void DrawInspector();
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal class CustomComponentInspectorAttribute : Attribute
    {
        private Type _inspectedType;

        public CustomComponentInspectorAttribute(Type inspectedType)
        {
            _inspectedType = inspectedType;
        }

        public Type InspectedType => _inspectedType;
    }
}
