namespace Editor.Rendering
{
    internal sealed class RenderPathManager
    {
        private IRenderPath _primary;

        internal RenderPathManager(RenderingManager rendering)
        {
            _primary = rendering.RenderPath;
        }


    }

}
