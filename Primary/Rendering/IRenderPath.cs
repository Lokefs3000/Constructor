namespace Primary.Rendering
{
    public interface IRenderPath
    {
        public void PreRenderPassSetup(RenderingManager manager);

        public void Install(RenderingManager manager);
        public void Uinstall(RenderingManager manager);
    }
}
