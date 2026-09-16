using Primary.Rendering.Resources;
using Primary.Scenes;
using Primary.Windowing;
using System.Numerics;

namespace Primary.Rendering.Data
{
    public sealed class RenderWindowData : IContextItem
    {
        public Window Window { get; internal set; }

        public FGTexture ColorTexture { get; internal set; }

        internal RenderWindowData()
        {
            Window = null!;
        }
    }
}
