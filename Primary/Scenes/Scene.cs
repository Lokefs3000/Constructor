using Arch.Core;
using Arch.Core.Extensions;
using Primary.Components;
using Primary.Editor;
using Serilog;

namespace Primary.Scenes
{
    public sealed class Scene : IDisposable
    {
        private readonly int _id;
        private readonly string _name;

        private readonly World _world;
        private readonly SceneEntityManager _entityManager;

        private readonly SceneEntity _root;

        private bool _disposedValue;

        internal Scene(int id, string name, World world, SceneEntityManager entityManager)
        {
            _id = id;
            _name = name;

            _world = world;
            _entityManager = entityManager;

            _root = _entityManager.CreateReadyEntity();
            _root.WrappedEntity.Add(new SceneTagComponent(_id));

            _root.Name = id.ToString();
            _root.AddComponent<Transform>();
        }

        public SceneEntity CreateEntity(SceneEntity parent)
        {
            SceneEntity newEntity = _entityManager.CreateReadyEntity();
            newEntity.WrappedEntity.Add(new SceneTagComponent(_id));
            newEntity.Parent = _root;

            return newEntity;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    //_root.Destroy();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [InspectorHidden]
        internal readonly record struct SceneTagComponent(int SceneId) : IComponent;

        internal int Id => _id;
        public SceneEntity Root => _root;

        public string Name => _name;
    }
}
