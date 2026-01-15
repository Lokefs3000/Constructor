using Editor.Assets;
using Editor.UI.Assets;
using Editor.UI.Assets.Loaders;
using Editor.UI.Visual;
using Primary;
using Primary.Assets;
using Primary.Common;
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

        private List<UIDockHost> _activeDockHosts;

        public UIManager(ILogger? logger)
        {
            Debug.Assert(s_instance.Target == null);
            s_instance.Target = this;

            _logger = logger;

            _windowManager = new UIWindowManager();
            _layoutManager = new UILayoutManager();
            _fontManager = new UIFontManager();
            _renderer = new UIRenderer(this);

            _activeDockHosts = new List<UIDockHost>();

            Engine.GlobalSingleton.AssetManager.RegisterCustomAsset<UIFontAsset>(new UIFontAssetLoader());
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
                            if (FlagUtility.HasFlag(dockHost.InvalidationFlags, UIInvalidationFlags.Layout))
                            {
                                dockHost.RemoveInvalidFlags(UIInvalidationFlags.Layout);
                                _layoutManager.RecalculateLayout(dockHost);
                            }
                        }
                    }
                }

                for (int i = 0; i < _activeDockHosts.Count; i++)
                {
                    UIDockHost dockHost = _activeDockHosts[i];
                    if (dockHost.ActiveWindow != null && FlagUtility.HasFlag(dockHost.InvalidationFlags, UIInvalidationFlags.Visual))
                    {
                        _renderer.AddHostToRedrawQueue(dockHost);
                    }
                }

                _fontManager.RenderPendingFonts();
                _renderer.PrepareForRendering();
            }
        }

        public UIDockHost CreateHostedDock(Window hostWindow)
        {
            UIDockHost dockHost = new UIDockHost();
            dockHost.SetupAsHosted(hostWindow);

            _activeDockHosts.Add(dockHost);
            return dockHost;
        }

        public UIDockHost CreateFloatingDock(Vector2 clientSize)
        {
            UIDockHost dockHost = new UIDockHost();
            dockHost.SetupAsFloating(clientSize);

            _activeDockHosts.Add(dockHost);
            return dockHost;
        }

        public UIDockHost CreateDockedHost(UIDockHost parentHost, UIDockSide side)
        {
            UIDockHost dockHost = new UIDockHost();
            dockHost.SetupAsDocked(parentHost, side);

            _activeDockHosts.Add(dockHost);
            return dockHost;
        }

        public T OpenWindow<T>(UIDockHost? host) where T : UIWindow
        {
            T window = _windowManager.OpenWindow<T>();

            host?.DockNewWindow(window);
            return window;
        }

        public IReadOnlyList<UIDockHost> ActiveHosts => _activeDockHosts;

        public UIWindowManager WindowManager => _windowManager;
        public UILayoutManager LayoutManager => _layoutManager;
        public UIFontManager FontManager => _fontManager;
        public UIRenderer Renderer => _renderer;

        public static UIManager Instance => Unsafe.As<UIManager>(s_instance.Target!);
        internal static ILogger? Logger => Instance._logger;
    }

    public enum UIInvalidationFlags : byte
    {
        None = 0,

        Layout = 1 << 0,
        Visual = 1 << 1,

        All = Layout | Visual
    }
}
