using Primary.Rendering.Assets;

namespace Primary.Components.Rendering
{
    [IncompatibleComponents(typeof(RenderMeshFilter))]
    public struct MeshSourceFilter : IComponent
    {
        public IRenderMeshSource? Source;
    }
}
