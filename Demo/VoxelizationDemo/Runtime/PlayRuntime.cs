using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Primary;
using Primary.Mathematics;
using Primary.Scripting;
using Primary.Windowing;
using PrimaryEditor.Assets;
using PrimaryEditor.Inspector;
using VoxelizationDemo.Editor;

namespace VoxelizationDemo.Runtime
{
    public sealed class PlayRuntime : Engine
    {
        private readonly RunningCommunication _runningCommunication;
        private readonly ScriptContext _scriptContext;

        public PlayRuntime(ReadOnlySpan<string> args) : base(args)
        {
            _runningCommunication = new RunningCommunication();
            _scriptContext = _scriptingManager.CreateContext("User");
        }

        public override void Dispose()
        {
            _runningCommunication.Dispose();

            base.Dispose();
        }

        public void Run()
        {
            Window window = WindowManager.CreateWindow("Runtime", new Int2(1336, 726), CreateWindowFlags.Resizable);

            while (!window.IsClosed)
            {
                _time.BeginNewFrame();
                _profilingManager.StartProfilingForFrame();

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

        public static PlayRuntime Instance => Unsafe.As<PlayRuntime>(GlobalSingleton);
    }
}
