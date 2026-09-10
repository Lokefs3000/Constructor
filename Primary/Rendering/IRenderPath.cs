namespace Primary.Rendering
{
    public interface IRenderPath
    {
        public void PreRenderPassSetup(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context);

        public void Install(RenderingManager manager);
        public void Uninstall(RenderingManager manager);
    }
}
