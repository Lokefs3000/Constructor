using Arch.Core;
using Primary.Assets;
using Primary.Components;

namespace Primary.Scenes
{
    public sealed class SceneManager : IDisposable
    {
        private World _world;
        private List<Scene> _scenes;

        private SceneEntityManager _entityManager;
        private SceneDeserializer _deserializer;

        private bool _disposedValue;

        internal SceneManager()
        {
            _world = World.Create();
            _scenes = new List<Scene>();

            _entityManager = new SceneEntityManager(_world);
            _deserializer = new SceneDeserializer();

            RegisterComponentsDefault.RegisterDefault();
        }

        /// <summary>Not thread-safe</summary>
        private int CreateSceneId()
        {
            while (true)
            {
                int num = Random.Shared.Next();
                if (num != int.MinValue && !_scenes.Exists((x) => x.Id == num))
                    return num;
            }
        }

        /// <summary>Not thread-safe</summary>
        public Scene CreateScene(string name, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                for (int i = 0; i < _scenes.Count; i++)
                {
                    SceneUnloaded?.Invoke(_scenes[i]);
                    _scenes[i].Dispose();
                }

                _scenes.Clear();
            }

            Scene scene = new Scene(CreateSceneId(), name, _world, _entityManager);

            _scenes.Add(scene);

            SceneLoaded?.Invoke(scene);
            return scene;
        }

        /// <summary>Not thread-safe</summary>
        public Scene LoadScene(string path, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                for (int i = 0; i < _scenes.Count; i++)
                {
                    SceneUnloaded?.Invoke(_scenes[i]);
                    _scenes[i].Dispose();
                }

                _scenes.Clear();
            }

            Scene scene = new Scene(CreateSceneId(), Path.GetFileNameWithoutExtension(path), _world, _entityManager);

            string? source = AssetFilesystem.ReadString(path);
            if (source != null)
                _deserializer.Deserialize(source, scene);
            else
                EngLog.Scene.Error("Failed to read scene file string: {p}", path);

            _scenes.Add(scene);

            SceneLoaded?.Invoke(scene);
            return scene;
        }

        /// <summary>Not thread-safe</summary>
        public Scene? FindScene(int id)
        {
            int idx = _scenes.FindIndex((x) => x.Id == id);
            return idx == -1 ? null : _scenes[idx];
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
        public SceneDeserializer Deserializer => _deserializer;

        public IReadOnlyList<Scene> Scenes => _scenes;

        public event Action<Scene>? SceneLoaded;
        public event Action<Scene>? SceneUnloaded;
    }

    public enum LoadSceneMode : byte
    {
        Single = 0,
        Additive
    }
}
