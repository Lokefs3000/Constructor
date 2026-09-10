using System;
using System.Collections.Generic;
using System.Text;
using Primary.Scenes;
using VoxelizationDemo.Core;

namespace VoxelizationDemo.Editor.Rendering
{
    internal sealed class EditorState
    {
        private Scene? _primaryScene;

        internal EditorState()
        {
            _primaryScene = null;

            SceneManager sceneManager = VoxelRuntime.Instance.SceneManager;

            sceneManager.SceneLoaded += (scene) =>
            {
                _primaryScene = scene;
            };

            sceneManager.SceneUnloaded += (scene) =>
            {
                if (_primaryScene == scene)
                    _primaryScene = null;
            };
        }

        public Scene? PrimaryScene { get => _primaryScene; set => _primaryScene = value; }
    }
}
