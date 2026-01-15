using Arch.Core;
using Primary.Components;
using System.Numerics;

namespace Editor.Rendering.Debugging
{
    internal sealed class EntityDebugRenderer
    {
        internal void Render()
        {
            World world = Editor.GlobalSingleton.SceneManager.World;
            if (RenderDebug.DrawEntityBounds)
            {
                world.InlineQuery<DrawRenderBoundsJob, RenderBounds>(DrawRenderBoundsJob.Query);
            }
        }

        private struct DrawRenderBoundsJob : IForEach<RenderBounds>
        {
            public void Update(ref RenderBounds bounds)
            {
                //Gizmos.DrawWireCube(bounds.ComputedBounds, Vector4.One);
            }

            public static readonly QueryDescription Query = new QueryDescription().WithAll<RenderBounds>();
        }
    }
}
