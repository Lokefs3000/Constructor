using Primary.Rendering.Assets;

namespace Primary.Components.Rendering
{
    [IncompatibleComponents(typeof(MeshSourceFilter))]
    public struct RenderMeshFilter : IComponent
    {
        public RawRenderMesh? Source;
    }
}
