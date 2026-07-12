using Primary.Assets;
using Primary.Assets.Types;
using Primary.Console;
using Primary.Input;
using Primary.Polling;
using Primary.Profiling;
using Primary.Scenes;
using Primary.Systems;
using Primary.Threading;
using Primary.Timing;
using Primary.Rendering;
using System.Runtime.CompilerServices;
using Primary.GUI.ImGui;
using Primary.Scripting;
using Primary.Windowing;
using Primary.Logging;
using System.Diagnostics;

namespace Primary
{
    /// <summary>
    /// Responsible for handling the core structures of the engine.
    /// </summary>
    public abstract class Engine : IDisposable
    {
        private static Engine? s_instance = null;

        private Logbook _logbook;
        private ThreadHelper _threadHelper;
        private ScriptingManager _scriptingManager;
        private ConsoleManager _consoleManager;
        private Time _time;
        private ProfilingManager _profilingManager;
        private AssetFilesystem _assetFilesystem;
        private AssetManager _assetManager;
        private EventManager _eventManager;
        private WindowManager _windowManager;
        private SceneManager _sceneManager;
        private RenderingManager _renderingManager;
        private SystemManager _systemManager;
        private InputSystem _inputSystem;
        private ImGuiManager _imguiManager;

        public Engine(ReadOnlySpan<string> args)
        {
            Debug.WriteLine("I'm a debug (^u^)!");

            s_instance = this;

            AppArguments.Parse(args);

            Thread.CurrentThread.Name = "Main";
            SystemHelper.EnsureFeaturesPresent();

            SDL.SDL3.SDL_Init(SDL.SDL_InitFlags.SDL_INIT_VIDEO | SDL.SDL_InitFlags.SDL_INIT_EVENTS);

            _logbook = new Logbook();

            PreInitialization();

            _threadHelper = new ThreadHelper();
            _scriptingManager = new ScriptingManager();
            _consoleManager = new ConsoleManager();
            _time = new Time();
            _profilingManager = new ProfilingManager();
            _assetFilesystem = new AssetFilesystem(); SetupFilesystems();
            _assetManager = new AssetManager(); SetupAssets();
            _eventManager = new EventManager();
            _windowManager = new WindowManager();
            _sceneManager = new SceneManager();
            _renderingManager = new RenderingManager();
            _systemManager = new SystemManager();
            _inputSystem = new InputSystem();
            _imguiManager = new ImGuiManager();
        }

        public virtual void Dispose()
        {
            _imguiManager.Dispose();
            _assetManager.Dispose();
            _renderingManager.Dispose();
            _sceneManager.Dispose();
            _windowManager.Dispose();
            _eventManager.Dispose();
            _profilingManager.Dispose();
            _scriptingManager.Dispose();

            s_instance = null;
        }

        #region Implementation API
        protected internal virtual void PreInitialization()
        {
        }

        protected internal virtual void SetupFilesystems()
        {
        }

        protected internal virtual void SetupAssets()
        {
        }
        #endregion

        public Logbook Logbook => _logbook;
        public ScriptingManager ScriptingManager => _scriptingManager;
        public ConsoleManager ConsoleManager => _consoleManager;
        public Time Time => _time;
        public ProfilingManager ProfilingManager => _profilingManager;
        public AssetFilesystem AssetFilesystem => _assetFilesystem;
        public AssetManager AssetManager => _assetManager;
        public EventManager EventManager => _eventManager;
        public WindowManager WindowManager => _windowManager;
        public SceneManager SceneManager => _sceneManager;
        public RenderingManager RenderingManager => _renderingManager;
        public SystemManager SystemManager => _systemManager;
        public ThreadHelper ThreadHelper => _threadHelper;
        public InputSystem InputSystem => _inputSystem;
        public ImGuiManager ImGuiManager => _imguiManager;

        public static Engine GlobalSingleton => s_instance!;

#if DEBUG
        public const bool IsDebugBuild = true;
#else
        public const bool IsDebugBuild = false;
#endif

#if AOT
        public const bool IsAOTBuild = true;
#else
        public const bool IsAOTBuild = false;
#endif
    }
}
