using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.IO;
using Primary.Scenes;
using PrimaryEditor.Assets;
using PrimaryEditor.Project;
using VoxelizationDemo.Core;
using VoxelizationDemo.Editor.Serialization;

namespace VoxelizationDemo.Editor
{
    internal sealed class ShortcutsManager : IDisposable
    {
        private bool _disposedValue;

        internal ShortcutsManager()
        {
            KeyboardDevice keyboard = InputSystem.Keyboard;
            keyboard.KeyReleased += OnKeyReleasedCallback;
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    KeyboardDevice keyboard = InputSystem.Keyboard;
                    keyboard.KeyReleased -= OnKeyReleasedCallback;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void OnKeyReleasedCallback(KeyCode key)
        {
            KeyboardDevice keyboard = InputSystem.Keyboard;

            switch (key)
            {
                case KeyCode.S:
                    {
                        if (keyboard.KeyModifiers.HasAny(KeyModifier.Control))
                        {
                            SceneManager sceneManager = VoxelRuntime.Instance.SceneManager;
                            SceneTracker sceneTracker = VoxelRuntime.Instance.EditorManager.SceneTracker;
                            
                            foreach (Scene scene in sceneManager.Scenes)
                            {
                                if (!sceneTracker.SceneSourcePaths.TryGetValue(scene, out string? localPath))
                                {
                                TrySaveSceneAgain:
                                    SaveFileDialogResult result = FileDialog.SaveFile(new SaveFileDialogParams
                                    {
                                        DefaultDirectory = ProjectData.Instance!.Paths.ContentFolder,
                                        Filters = [new FileFilter("Scene files", "*.scene")]
                                    });

                                    if (result.Result == FileDialogResult.Ok)
                                    {
                                        if (!FilesystemManager.TryGetLocalPath(result.File, out localPath))
                                            continue;

                                        foreach (var (_, sourcePath) in sceneTracker.SceneSourcePaths)
                                        {
                                            if (sourcePath == localPath)
                                            {
                                                goto TrySaveSceneAgain;
                                            }
                                        }

                                        sceneTracker.UpdateScenePath(scene, localPath);
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                if (!FilesystemManager.TryGetFullPath(localPath, out string? fullPath))
                                    continue;

                                try
                                {
                                    using Stream stream = FileUtility.TryWaitOpen(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4, 100);
                                    SceneSerializer.Serialize(stream, scene);
                                }
                                catch (Exception ex)
                                {
                                    AppLog.Editor.Error(ex, "Failed to serialize scene '{p}'", localPath);
                                }
                            }
                        }

                        break;
                    }
            }
        }
    }
}
