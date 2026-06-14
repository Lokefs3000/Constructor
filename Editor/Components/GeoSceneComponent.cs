using Editor.Assets.Types;
using Primary.Components;
using System.Runtime.Serialization;

namespace Editor.Components
{
    [Component]
    public record struct GeoSceneComponent : IComponent
    {
        private GeoSceneAsset? _scene;

        [IgnoreDataMember] private long _updateIndex;
        [IgnoreDataMember] private DateTime _lastUpdateTime;

        public GeoSceneComponent()
        {
            _scene = null;

            _updateIndex = long.MinValue;
            _lastUpdateTime = DateTime.MinValue;
        }

        public GeoSceneAsset? Scene { get => _scene; set { _scene = value; _updateIndex = long.MinValue; _lastUpdateTime = DateTime.MinValue; } }
        
        public long UpdateIndex { get => _updateIndex; set => _updateIndex = value; }
        public DateTime LastUpdateTime { get => _lastUpdateTime; set => _lastUpdateTime = value; }
    }
}
