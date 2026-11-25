namespace Editor.Gui.View.Toolbars
{
    internal sealed class ToolSpaceToolbar : IEditorViewToolbar
    {
        public ToolbarGroup SetupToolbar(ToolbarSetupContext context)
        {
            ToolbarGroup group = context.LoadGroup("Editor/UI/EditorView/Toolbar/Toolspace.json");

            return group;
        }

        public void CleanupToolbar(ToolbarGroup group)
        {

        }

        public void Initialize(bool isBeingReloaded)
        {
            throw new NotImplementedException();
        }

        public void Cleanup()
        {
            throw new NotImplementedException();
        }
    }
}
