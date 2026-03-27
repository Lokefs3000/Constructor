using Editor.UI.Assets;
using Editor.UI.Assets.Loaders;
using Editor.UI.Reflection;
using Editor.UI.Serialization;
using Editor.UI.Styling;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary;
using Primary.Assets;
using Primary.Common;
using Primary.Mathematics;
using Primary.Profiling;
using Primary.Rendering;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.UI
{
    public sealed class UIManager
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

        private List<UIDockHost> _activeDockHosts;
        private bool _areSomeHostsInvalid;

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

            _activeDockHosts = new List<UIDockHost>();
            _areSomeHostsInvalid = false;

            Engine.GlobalSingleton.AssetManager.RegisterCustomAsset<UIFontAsset>(new UIFontAssetLoader());
            Engine.GlobalSingleton.AssetManager.RegisterCustomAsset<StylesheetAsset>(new StylesheetAssetLoader());
        }

        public void UpdatePendingLayouts()
        {
            using (new ProfilingScope("UIUpdate"))
            {
                using (new ProfilingScope("Layout"))
                {
                    for (int i = 0; i < _activeDockHosts.Count; i++)
                    {
                        UIDockHost dockHost = _activeDockHosts[i];
                        if (dockHost.ParentHost == null)
                        {
                            if (Flags.HasFlag(dockHost.InvalidationFlags, UIStateFlags.InvalidLayout))
                            {
                                _layoutManager.AddInvalidLayout(dockHost);
                            }
                        }
                    }
                }

                for (int i = 0; i < _activeDockHosts.Count; i++)
                {
                    UIDockHost dockHost = _activeDockHosts[i];
                    if (Flags.HasFlag(dockHost.InvalidationFlags, UIStateFlags.InvalidVisual))
                    {
                        _renderer.AddHostToRedrawQueue(dockHost);
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

        public UIDockHost? FindDockHostFromWindowId(uint windowId)
        {
            foreach (UIDockHost dockHost in _activeDockHosts)
            {
                if (dockHost.Window != null && dockHost.Window.WindowId == windowId)
                    return dockHost;
            }

            return null;
        }

        public T OpenWindow<T>(UIDockHost? host, string? layoutFile) where T : UIWindow
        {
            T window = _windowManager.OpenWindow<T>();

            if (layoutFile != null)
                _serilizationManager.DeserializeLayout(window, layoutFile);

            host?.DockNewWindow(window);
            window.CallPostLoadInit();

            return window;
        }

        internal void SetAnyHostAsDirty() => _areSomeHostsInvalid = true;

        public IReadOnlyList<UIDockHost> ActiveHosts => _activeDockHosts;

        public UIWindowManager WindowManager => _windowManager;
        public UILayoutManager LayoutManager => _layoutManager;
        public UIFontManager FontManager => _fontManager;
        public UIRenderer Renderer => _renderer;
        public ReflectionManager ReflectionManager => _reflectionManager;
        public StyleManager StyleManager => _styleManager;
        public SerializationManager SerializationManager => _serilizationManager;
        public TextManager TextManager => _textManager;

        public static UIManager Instance => Unsafe.As<UIManager>(s_instance.Target!);
        internal static ILogger? Logger => Instance._logger;
    }

    public enum UIStateFlags : byte
    {
        None = 0,

        InvalidLayout = 1 << 0,
        InvalidVisual = 1 << 1,

        InvalidAll = InvalidLayout | InvalidVisual
    }
}
