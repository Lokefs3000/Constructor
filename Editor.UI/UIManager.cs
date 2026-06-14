using Editor.UI.Assets;
using Editor.UI.Assets.Loaders;
using Editor.UI.Diagnostics;
using Editor.UI.Layout;
using Editor.UI.Menu;
using Editor.UI.Popup;
using Editor.UI.Reflection;
using Editor.UI.Serialization;
using Editor.UI.Styling;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary;
using Primary.Assets;
using Primary.Collections;
using Primary.Common;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Windowing;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.UI
{
    public sealed class UIManager : IDisposable
    {
        private static WeakReference s_instance = new WeakReference(null);

        private ILogger? _logger;

        private UIWindowManager _windowManager;
        private UILayoutManager _layoutManager;
        private UIFontManager _fontManager;
        private UIRenderer _renderer;
        private UIInteractionManager _interactionManager;
        private ReflectionManager _reflectionManager;
        private StyleManager _styleManager;
        private SerializationManager _serilizationManager;
        private TextManager _textManager;
        private TextEditManager _textEditManager;
        private LayoutSnippetManager _layoutSnippetManager;
        private PopupManager _popupManager;
        private ContextMenu _contextMenu;

        private List<IInterfaceHost> _activeDockHosts;
        private bool _areSomeHostsInvalid;

        private bool _disposedValue;

        public UIManager(ILogger? logger)
        {
            Debug.Assert(s_instance.Target == null);
            s_instance.Target = this;

            _logger = logger;

            _windowManager = new UIWindowManager();
            _layoutManager = new UILayoutManager();
            _fontManager = new UIFontManager();
            _renderer = new UIRenderer(this);
            _interactionManager = new UIInteractionManager(this);
            _reflectionManager = new ReflectionManager();
            _styleManager = new StyleManager(this);
            _serilizationManager = new SerializationManager();
            _textManager = new TextManager();
            _textEditManager = new TextEditManager();
            _layoutSnippetManager = new LayoutSnippetManager(this);
            _popupManager = new PopupManager();
            _contextMenu = new ContextMenu();

            _activeDockHosts = new List<IInterfaceHost>();
            _areSomeHostsInvalid = false;

            Engine.GlobalSingleton.ImGuiManager.AddDrawer(new UIManagerDebug());

            Engine.GlobalSingleton.AssetManager.RegisterCustomAsset<UIFontAsset>(new UIFontAssetLoader());
            Engine.GlobalSingleton.AssetManager.RegisterCustomAsset<StylesheetAsset>(new StylesheetAssetLoader());
            Engine.GlobalSingleton.AssetManager.RegisterCustomAsset<ContextMenuAsset>(new ContextMenuAssetLoader());
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _windowManager.Dispose();

                    foreach (IInterfaceHost host in _activeDockHosts)
                    {
                        if (host is IDisposable disposable)
                            disposable.Dispose();
                    }

                    _contextMenu.Dispose();
                    _textManager.Dispose();
                    _interactionManager.Dispose();
                    _fontManager.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void UpdatePendingLayouts()
        {
            using (new ProfilingScope("UpdateUI"))
            {
                OnPreUpdateUI?.Invoke();

                using (new ProfilingScope("Update"))
                {
                    for (int i = 0; i < _activeDockHosts.Count; i++)
                    {
                        IInterfaceHost host = _activeDockHosts[i];
                        using (new ProfilingScope("UpdateHost"))
                        {
                            host.Update();
                        }
                    }
                }

                using (new ProfilingScope("Layout"))
                {
                    for (int i = 0; i < _activeDockHosts.Count; i++)
                    {
                        IInterfaceHost host = _activeDockHosts[i];
                        if (host is ILayoutHost layoutHost)
                        {
                            if (layoutHost is IWindowDockHost windowHost && windowHost.ParentHost != null)
                                continue;

                            if (Flags.HasFlag(host.InvalidationFlags, UIStateFlags.InvalidLayout))
                            {
                                _layoutManager.AddInvalidLayout(layoutHost);
                            }
                        }
                    }
                }

                for (int i = 0; i < _activeDockHosts.Count; i++)
                {
                    IInterfaceHost host = _activeDockHosts[i];
                    if (host is IRenderableHost renderableHost)
                    {
                        if (Flags.HasFlag(renderableHost.InvalidationFlags, UIStateFlags.InvalidVisual))
                        {
                            _renderer.AddHostToRedrawQueue(renderableHost);
                        }
                    }
                }

                _styleManager.RefreshStyles();

                _layoutManager.RecalculateAll();

                _renderer.PrepareForRendering();
                _textManager.ShapeAllDeferredTextData();
                _fontManager.RenderPendingFonts();

                if (_renderer.HasUnbuiltDraws)
                {
                    _renderer.BuildDrawCommands();
                }

                _textManager.ReturnUsedData();
            }
        }

        private int GetUniqueHostId()
        {
            while (true)
            {
                int id = (int)Stopwatch.GetTimestamp();

                bool foundMatch = false;
                foreach (UIDockHost host in _activeDockHosts)
                {
                    if (host.UniqueDockHostId == id)
                    {
                        foundMatch = true;
                    }
                }

                if (!foundMatch)
                    return id;
            }
        }

        public UIDockHost CreateHostedDock(Window hostWindow)
        {
            UIDockHost dockHost = new UIDockHost(GetUniqueHostId());
            dockHost.SetupAsHosted(hostWindow);

            _activeDockHosts.Add(dockHost);
            return dockHost;
        }

        public UIDockHost CreateFloatingDock(Int2 clientSize)
        {
            UIDockHost dockHost = new UIDockHost(GetUniqueHostId());
            dockHost.SetupAsFloating(clientSize);

            _activeDockHosts.Add(dockHost);
            return dockHost;
        }

        public UIDockHost CreateDockedHost(UIDockHost parentHost, UIDockSide side)
        {
            UIDockHost dockHost = new UIDockHost(GetUniqueHostId());
            dockHost.SetupAsDocked(parentHost, side);

            _activeDockHosts.Add(dockHost);
            return dockHost;
        }

        public IInterfaceHost? FindDockHostFromWindowId(uint windowId)
        {
            foreach (IInterfaceHost dockHost in _activeDockHosts)
            {
                if (dockHost is IRenderableHost renderableHost && renderableHost.HostWindow != null && renderableHost.HostWindow.WindowId == windowId)
                    return dockHost;
            }

            return null;
        }

        public T OpenWindow<T>(UIDockHost? host, string? layoutFile) where T : UIWindow
        {
            T window = _windowManager.OpenWindow<T>();

            if (layoutFile != null)
            {
                DateTime startTime = DateTime.Now;
                _serilizationManager.DeserializeLayout(window, layoutFile);

                Logger?.Debug("Loading layout file {f} took {t:f2}s", layoutFile, (DateTime.Now - startTime).TotalSeconds);
            }

            host?.DockNewWindow(window);
            window.PostLoadInit(layoutFile);

            OnWindowOpened?.Invoke(window);
            _windowManager.FocusWindow(window);
            return window;
        }

        public T? FindWindow<T>() where T : UIWindow
        {
            return _windowManager.FindWindow<T>();
        }

        public T CreateSnippet<T>(string snippetFile, params object?[]? arguments) where T : LayoutSnippet
        {
            DateTime startTime = DateTime.Now;
            T snippet = _layoutSnippetManager.LoadNewSnippet<T>(snippetFile, arguments);

            Logger?.Debug("Loading snippet file {f} took {t:f2}s", snippetFile, (DateTime.Now - startTime).TotalSeconds);
            return snippet;
        }

        public void ReloadWindowLayouts(string layoutFile)
        {
            Logger?.Debug("Reloading all open windows with layout file: {f}", layoutFile);

            using RentedList<UIWindow> windowsToReload = new RentedList<UIWindow>();

            foreach (var kvp in _windowManager.Active)
            {
                if (kvp.Value.LayoutFile == layoutFile)
                    windowsToReload.Add(kvp.Value);
            }

            if (!windowsToReload.IsEmpty)
            {
                foreach (UIWindow window in windowsToReload)
                {
                    window.RootElement.ClearChildren();

                    DateTime startTime = DateTime.Now;
                    _serilizationManager.DeserializeLayout(window, layoutFile);

                    Logger?.Debug("Loading layout file {f} took {t:f2}s", layoutFile, (DateTime.Now - startTime).TotalSeconds);

                    window.Cleanup();
                    window.PostLoadInit(layoutFile);
                    window.InvalidateTree(UIStateFlags.InvalidAll);

                    OnWindowReloaded?.Invoke(window);
                    if (_windowManager.FocusedWindow == window)
                        ((IWindow)window).OnFocusGainedCallback();
                }
            }
        }

        public void ReloadWindowSnippets(string snippetFile)
        {
            Logger?.Debug("Reloading all loaded snippets with file: {f}", snippetFile);

            _layoutSnippetManager.ReloadAll(snippetFile);
        }

        internal void SetAnyHostAsDirty() => _areSomeHostsInvalid = true;

        internal void AddPopupAsHost(PopupHost host) => _activeDockHosts.Add(host);
        internal void RemovePopupAsHost(PopupHost host) => _activeDockHosts.Remove(host);

        internal void AddContextMenuAsHost(ContextMenuHost host) => _activeDockHosts.Add(host);
        internal void RemoveContextMenuAsHost(ContextMenuHost host) => _activeDockHosts.Remove(host);

        public IReadOnlyList<IInterfaceHost> ActiveHosts => _activeDockHosts;

        public UIWindowManager WindowManager => _windowManager;
        public UILayoutManager LayoutManager => _layoutManager;
        public UIFontManager FontManager => _fontManager;
        public UIRenderer Renderer => _renderer;
        public UIInteractionManager InteractionManager => _interactionManager;
        public ReflectionManager ReflectionManager => _reflectionManager;
        public StyleManager StyleManager => _styleManager;
        public SerializationManager SerializationManager => _serilizationManager;
        public TextManager TextManager => _textManager;
        public TextEditManager TextEditManager => _textEditManager;
        public LayoutSnippetManager LayoutSnippetManager => _layoutSnippetManager;
        public PopupManager PopupManager => _popupManager;
        public ContextMenu ContextMenu => _contextMenu;

        public event Action? OnPreUpdateUI;

        public event Action<UIWindow>? OnWindowOpened;
        public event Action<UIWindow>? OnWindowClosed;
        public event Action<UIWindow>? OnWindowReloaded;

        public static UIManager Instance => Unsafe.As<UIManager>(s_instance.Target!);
        internal static ILogger? Logger => Instance._logger;
    }

    [Flags]
    public enum UIStateFlags : byte
    {
        None = 0,

        InvalidLayout = 1 << 0,
        InvalidVisual = 1 << 1,

        InvalidAll = InvalidLayout | InvalidVisual
    }
}
