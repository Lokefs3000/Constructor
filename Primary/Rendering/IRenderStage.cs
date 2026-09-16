namespace Primary.Rendering
{
    public interface IRenderStage
    {
        public void FlushPassesForCamera(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context);
        public void FlushPassesForWindow(RenderingManager manager, RenderPassBlackboard blackboard, RenderContextContainer context);

        public void Install(RenderingManager manager);
        public void Uninstall(RenderingManager manager);
    }
}
