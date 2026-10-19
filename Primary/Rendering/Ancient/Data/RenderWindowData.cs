using Primary.Rendering.Ancient.Resources;
using Primary.Scenes;
using Primary.Windowing;
using System.Numerics;

namespace Primary.Rendering.Ancient.Data
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
