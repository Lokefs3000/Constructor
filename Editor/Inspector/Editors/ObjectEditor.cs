namespace Editor.Inspector.Editors
{
    public abstract class ObjectEditor
    {
        public abstract void SetupInspectorFields(object obj);
        public abstract void DrawInspector();
    }
}
