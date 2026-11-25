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
using Primary.Scripting;
using Primary.Threading;
using Primary.Windowing;
using PrimaryEditor.Assets;
using PrimaryEditor.Inspector;
using PrimaryEditor.Project;
using PrimaryEditor.Scripting;
using VoxelizationDemo.Assets;
using VoxelizationDemo.Assets.Importers;
using VoxelizationDemo.Assets.Loaders;
using VoxelizationDemo.Components;
using VoxelizationDemo.Diagnostics;
using VoxelizationDemo.Editor;
using VoxelizationDemo.Procedural;
using VoxelizationDemo.Rendering;

namespace VoxelizationDemo.Core
{
    public sealed class VoxelRuntime : Engine
    {
        private RuntimeState _state;

        private ProjectData _projectData;
        private AssetPipeline _assetPipeline;
        private ScriptPipeline _scriptPipeline;

        private readonly InspectorManager _inspectorManager;

        private readonly EditorManager _editorManager;

        private ScriptContext _scriptContext;

        internal VoxelRuntime(ReadOnlySpan<string> args) : base(args)
        {
            _state = RuntimeState.Editor;

            _inspectorManager = new InspectorManager();

            _editorManager = new EditorManager();

            _scriptContext = _scriptingManager.CreateContext("User");
        }

        public override void Dispose()
        {
            _scriptContext.Dispose();
            _editorManager.Dispose();

            base.Dispose();
        }

        internal void Run()
        {
            Window window = _windowManager.CreateWindow("Voxel", new Int2(1600, 900), CreateWindowFlags.Resizable);

            _renderingManager.InstallRenderStage(new WorldRenderStage());

            _scriptingManager.TypeLoader.LoadTypesOnAssembly(typeof(VoxelRuntime).Assembly);
            _scriptContext.LoadAssembly(FilesystemManager.GetFullPath("Library/Binary/Game.dll")!);

            // {
            //     SceneEntityManager.Instance.RegisterComponent<PointLight>();
            //     SceneEntityManager.Instance.RegisterComponent<Components.DirectionalLight>();
            //     SceneEntityManager.Instance.RebuildDependencyGraph();
            // }

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

                _scriptingManager.ProcessPendingScripts();

                _inputSystem.UpdatePending();
                _eventManager.PollEvents();

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
            _scriptPipeline = new ScriptPipeline();

            _assetPipeline.ImporterRegistry.RegisterImporter<EffectVolumeImporter>(".fxvol");

            _projectData.SetupData(@"..\..\..\Source", null);
            _assetPipeline.ImportAnyChangesLaunch(null);
            _scriptPipeline.PerformLaunchCheck();
        }

        protected override void SetupFilesystems()
        {
            AssetFilesystem.AddFilesystem(_assetPipeline.FilesystemManager);
        }

        protected override void SetupAssets()
        {
            AssetManager.LockInIdProvider(_assetPipeline.AssetRegistry);

            AssetManager.RegisterCustomAsset<EffectVolumeAsset>(new EffectVolumeLoader());
        }

        internal void ChangeRuntimeState(RuntimeState state)
        {
            if (_state != state)
            {
                _state = state;
                OnRuntimeStateChanged?.Invoke(state);
            }
        }

        public RuntimeState State => _state;

        internal InspectorManager InspectorManager => _inspectorManager;

        internal EditorManager EditorManager => _editorManager;

        internal ScriptContext ScriptContext { get => _scriptContext; set => _scriptContext = value; }

        public event Action<RuntimeState>? OnRuntimeStateChanged;

        public static VoxelRuntime Instance => Unsafe.As<VoxelRuntime>(GlobalSingleton);
    }

    public enum RuntimeState : byte
    {
        Editor = 0,
        Running
    }
}
