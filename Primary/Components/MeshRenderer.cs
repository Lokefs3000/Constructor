using Primary.Assets;
using Primary.Editor;
using Primary.Rendering.Data;
using Primary.Scenes;
using Primary.Timing;
using System.Runtime.Serialization;

namespace Primary.Components
{
    [ComponentConnections(typeof(RenderableAdditionalData), typeof(RenderBounds))]
    [ComponentUsage(HasSelfReference: true)]
    public struct MeshRenderer : IComponent
    {
        [IgnoreDataMember]
        private SceneEntity _self;

        private RawRenderMesh? _mesh;
        private MaterialAsset? _material;

        private int _frameIndex;

        public RawRenderMesh? Mesh
        {
            get => _mesh;
            set
            {
                //if (_mesh != value)
                //    RenderFlagContainer.Instance.ChangeMesh(_self);
                if (_mesh != value)
                    _frameIndex = Time.FrameIndex;
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

        public int UpdateIndex => _frameIndex;
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
