using Primary.Assets;
using Primary.Editor;
using Primary.Rendering.Batching;
using Primary.Scenes;

namespace Primary.Components
{
    [ComponentConnections(typeof(RenderableAdditionalData))]
    [ComponentUsage(HasSelfReference: true)]
    public record struct MeshRenderer : IComponent
    {
        private SceneEntity _self;

        private RenderMesh? _mesh;
        private MaterialAsset? _material;

        public RenderMesh? Mesh
        {
            get => _mesh;
            set
            {
                //if (_mesh != value)
                //    RenderFlagContainer.Instance.ChangeMesh(_self);
                _mesh = value;
            }
        }
        public MaterialAsset? Material
        {
            get => _material;
            set
            {
                //if (_material != value)
                //    RenderFlagContainer.Instance.ChangeMaterial(_self);
                _material = value;
            }
        }
    }

    [ComponentUsage(CanBeAdded: false)]
    [InspectorHidden]
    internal record struct RenderableAdditionalData : IComponent
    {
        public uint BatchId;

        public RenderableAdditionalData()
        {
            BatchId = uint.MaxValue;
        }
    }
}
