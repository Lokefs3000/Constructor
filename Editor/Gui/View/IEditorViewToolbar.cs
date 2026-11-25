namespace Editor.Gui.View
{
    public interface IEditorViewToolbar
    {
        public void Initialize(bool isBeingReloaded);
        public void Cleanup();

        public ToolbarGroup SetupToolbar(ToolbarSetupContext context);
        public void CleanupToolbar(ToolbarGroup group);
    }
}
