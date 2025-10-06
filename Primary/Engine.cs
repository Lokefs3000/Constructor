using Primary.Assets;
using Primary.Console;
using Primary.Input;
using Primary.Polling;
using Primary.Profiling;
using Primary.Rendering;
using Primary.Scenes;
using Primary.Systems;
using Primary.Threading;
using Primary.Timing;
using Serilog;
using System.Runtime.CompilerServices;

namespace Primary
{
    /// <summary>
    /// Responsible for handling the core structures of the engine.
    /// </summary>
    public class Engine : IDisposable
    {
        private static Engine? s_instance = null;

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
        private ThreadHelper _threadHelper;
        private InputSystem _inputSystem;

        public Engine()
        {
            s_instance = this;
        }

        protected void Initialize(IAssetIdProvider assetIdProvider)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .CreateLogger();

            Thread.CurrentThread.Name = "Main";

            SDL.SDL3.SDL_Init(SDL.SDL_InitFlags.SDL_INIT_VIDEO | SDL.SDL_InitFlags.SDL_INIT_EVENTS);

            _consoleManager = new ConsoleManager();
            _time = new Time();
            _profilingManager = new ProfilingManager();
            _assetFilesystem = new AssetFilesystem(); SetupAssetFilesystem();
            _assetManager = new AssetManager(); _assetManager.LockInIdProvider(assetIdProvider);
            _eventManager = new EventManager();
            _windowManager = new WindowManager();
            _sceneManager = new SceneManager();
            _renderingManager = new RenderingManager();
            _systemManager = new SystemManager();
            _threadHelper = new ThreadHelper();
            _inputSystem = new InputSystem();
        }

        public virtual void Dispose()
        {
            _renderingManager.Dispose();
            _sceneManager.Dispose();
            _windowManager.Dispose();
            _eventManager.Dispose();
            _assetManager.Dispose();
            _profilingManager.Dispose();

            s_instance = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void SetupAssetFilesystem()
        {

        }

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

        public static Engine GlobalSingleton => s_instance!;
    }
}
