using Primary.Assets;
using Primary.Editor;
using Primary.Rendering.Assets;
using Primary.Scenes;
using Primary.Timing;
using System.Runtime.Serialization;

namespace Primary.Components
{
    [Component]
    [ComponentConnections(typeof(RenderBounds), typeof(RenderOctantInfo))]
    [ComponentUsage(HasSelfReference: true)]
    public struct MeshRenderer : IComponent
    {
        [IgnoreDataMember]
        private long _updateIndex;

        private MeshAsset? _mesh;
        private MaterialAsset? _material;

        private ulong _layers;

        public MeshRenderer()
        {
            _updateIndex = -1;

            _mesh = null;
            _material = null;

            _layers = ulong.MaxValue;
        }

        public MeshAsset? Mesh
        {
            get => _mesh;
            set
            {
                if (_mesh != value)
                    _updateIndex = Time.FrameIndex;
                _mesh = value;
            }
        }
        public MaterialAsset? Material
        {
            get => _material;
            set
            {
                if (_material != value)
                    _updateIndex = Time.FrameIndex;
                _material = value;
            }
        }

        public ulong Layers
        {
            get => _layers;
            set
            {
                if (_layers != value)
                    _updateIndex = Time.FrameIndex;
                _layers = value;
            }
        }

        [IgnoreDataMember]
        public readonly long UpdateIndex => _updateIndex;
    }
}
