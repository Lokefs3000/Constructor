using Editor.Inspector.Editors;
using Primary.Scenes;

namespace Editor.Inspector.Components
{
    internal abstract class ComponentEditor : ObjectEditor
    {
        public abstract void SetupInspectorFields(SceneEntity entity, Type type);

        public override void SetupInspectorFields(object @object) => throw new NotSupportedException();
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
