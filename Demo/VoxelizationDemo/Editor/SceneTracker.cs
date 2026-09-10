using System;
using System.Collections.Generic;
using System.Text;
using Primary.Collections.ReadOnly;
using Primary.Scenes;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Editor
{
    internal sealed class SceneTracker : IDisposable
    {
        private readonly Dictionary<Scene, string> _sceneSourcePaths;

        private bool _disposedValue;

        internal SceneTracker()
        {
            _sceneSourcePaths = new Dictionary<Scene, string>();

            SceneManager sceneManager = VoxelRuntime.Instance.SceneManager;
            sceneManager.SceneLoaded += OnSceneLoadedCallback;
            sceneManager.SceneUnloaded += OnSceneUnloadedCallback;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    SceneManager sceneManager = VoxelRuntime.Instance.SceneManager;
                    sceneManager.SceneLoaded -= OnSceneLoadedCallback;
                    sceneManager.SceneUnloaded -= OnSceneUnloadedCallback;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void OnSceneLoadedCallback(Scene scene)
        {
        }

        private void OnSceneUnloadedCallback(Scene scene)
        {
            _sceneSourcePaths.Remove(scene);
        }

        internal void UpdateScenePath(Scene scene, string localPath)
        {
            _sceneSourcePaths[scene] = localPath;
        }

        public RODictionary<Scene, string> SceneSourcePaths => _sceneSourcePaths;
    }
}
