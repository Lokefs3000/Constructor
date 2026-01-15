namespace Primary.Rendering.Data
{
    public sealed class RenderStateData : IContextItem
    {
        public IRenderPath Path { get; internal set; }

        internal RenderStateData()
        {

        }
    }
}
