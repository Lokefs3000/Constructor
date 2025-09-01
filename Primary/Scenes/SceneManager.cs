using Arch.Core;
using CommunityToolkit.HighPerformance;
using Primary.Components;
using System.Diagnostics;

namespace Primary.Scenes
{
    public sealed class SceneManager : IDisposable
    {
        private World _world;
        private List<Scene> _scenes;

        private SceneEntityManager _entityManager;

        private bool _disposedValue;

        internal SceneManager()
        {
            _world = World.Create();
            _scenes = new List<Scene>();

            _entityManager = new SceneEntityManager(_world);

            SerializerTypes.RegisterDefault();
            RegisterComponentsDefault.RegisterDefault();
        }

        private int CreateSceneId()
        {
            while (true)
            {
                int num = Random.Shared.Next();
                if (!_scenes.Exists((x) => x.Id == num))
                    return num;
            }
        }

        public Scene CreateScene(string name)
        {
            Scene scene = new Scene(CreateSceneId(), name, _world, _entityManager);

            _scenes.Add(scene);
            return scene;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                World.Destroy(_world);

                _disposedValue = true;
            }
        }

        ~SceneManager()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public World World => _world;
        internal SceneEntityManager EntityManager => _entityManager;

        public IReadOnlyList<Scene> Scenes => _scenes;
    }
}
