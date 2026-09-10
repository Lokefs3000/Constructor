using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary;
using Primary.Common;
using Primary.Components;
using Primary.Input;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Scenes;
using Primary.Scenes.Components;
using Primary.Windowing;
using PrimaryEditor.Assets;
using PrimaryEditor.Inspector;
using PrimaryEditor.Project;
using VoxelizationDemo.Components;
using VoxelizationDemo.Diagnostics;
using VoxelizationDemo.Editor;
using VoxelizationDemo.Logic;
using VoxelizationDemo.Rendering;

namespace VoxelizationDemo.Core
{
    public sealed class VoxelRuntime : Engine
    {
        private ProjectData _projectData;
        private AssetPipeline _assetPipeline;

        private readonly CameraManager _cameraManager;
        private readonly InspectorManager _inspectorManager;

        private readonly EditorManager _editorManager;

        internal VoxelRuntime(ReadOnlySpan<string> args) : base(args)
        {
            _cameraManager = new CameraManager();
            _inspectorManager = new InspectorManager();

            _editorManager = new EditorManager();
        }

        internal void Run()
        {
            Window window = _windowManager.CreateWindow("Voxel", new Int2(1600, 900), CreateWindowFlags.Resizable);

            _renderingManager.SetNewRenderPath(new CoreRenderPath());

            {
                SceneEntityManager.Instance.RegisterComponent<PointLight>();
                SceneEntityManager.Instance.RebuildDependencyGraph();
            }

            Scene scene = _sceneManager.CreateScene("Unique", LoadSceneMode.Single);
            _cameraManager.SetupWithinScene(scene);

            _sceneManager.CreateScene("Untitiled", LoadSceneMode.Additive);

            _editorManager.Enable();

            while (!window.IsClosed)
            {
                _time.BeginNewFrame();
                _profilingManager.StartProfilingForFrame();

                _assetPipeline.HandleUpdates();
                _inspectorManager.UpdateContexts();

                _editorManager.Update();

                _sceneManager.UpdateScenes();
                _threadHelper.ExecutePendingTasks();
                _assetManager.UpdateAssets();

                _inputSystem.UpdatePending();
                _eventManager.PollEvents();

                _cameraManager.UpdateCamera();
                _systemManager.RunSystems();

                _imguiManager.UpdateAndRender();
                _renderingManager.Render();
            }
        }

        protected override void PreInitialization()
        {
            AppArguments.TryAddValue("--render-draw-gizmos");

            _projectData = new ProjectData();
            _assetPipeline = new AssetPipeline(null);

            _projectData.SetupData(@"D:\source\repos\Constructor\Demo\VoxelizationDemo\Source", null);
            _assetPipeline.ImportAnyChangesLaunch(null);
        }

        protected override void SetupFilesystems()
        {
            AssetFilesystem.AddFilesystem(_assetPipeline.FilesystemManager);
        }

        protected override void SetupAssets()
        {
            AssetManager.LockInIdProvider(_assetPipeline.AssetRegistry);
        }

        internal CameraManager CameraManager => _cameraManager;
        internal InspectorManager InspectorManager => _inspectorManager;

        internal EditorManager EditorManager => _editorManager;

        public static VoxelRuntime Instance => Unsafe.As<VoxelRuntime>(GlobalSingleton);
    }
}
