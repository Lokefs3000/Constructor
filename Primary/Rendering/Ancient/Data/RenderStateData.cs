using Primary.Rendering.Ancient;

namespace Primary.Rendering.Ancient.Data
{
    public sealed class RenderStateData : IContextItem
    {
        public IRenderStage Path { get; internal set; }

        internal RenderStateData()
        {

        }
    }
}
