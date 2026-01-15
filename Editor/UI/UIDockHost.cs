using Primary;
using Primary.Common;
using Primary.Rendering;
using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI
{
    public sealed class UIDockHost
    {
        private UIDockHost? _parentHost;
        private UIDockSide _dockedSide;

        private Window? _hostWindow;
        private Vector2 _hostClientSize;
        private bool _isExternallyHosted;

        private RHITexture? _hostTexture;

        private List<UIWindow> _tabbedWindows;
        private Boundaries _tabbedClientSize;
        private int _activeTabbedWindow;

        private List<UIDockHost> _dockedHosts;

        private UIInvalidationFlags _invalidationFlags;

        public UIDockHost()
        {
            _parentHost = null;
            _dockedSide = UIDockSide.Left;

            _hostWindow = null;
            _hostClientSize = Vector2.Zero;
            _isExternallyHosted = false;

            _hostTexture = null;

            _tabbedWindows = new List<UIWindow>();
            _tabbedClientSize = Boundaries.Zero;
            _activeTabbedWindow = 0;

            _dockedHosts = new List<UIDockHost>();

            _invalidationFlags = UIInvalidationFlags.None;
        }

        internal void SetupAsFloating(Vector2 clientSize)
        {
            _hostWindow = Engine.GlobalSingleton.WindowManager.CreateWindow("Empty dock host", clientSize, CreateWindowFlags.Resizable);
            _hostClientSize = clientSize;

            _invalidationFlags = UIInvalidationFlags.All;
        }

        internal void SetupAsDocked(UIDockHost host, UIDockSide side)
        {
            host.DockNewHost(this, side);

            _invalidationFlags = UIInvalidationFlags.All;
        }

        internal void SetupAsHosted(Window window)
        {
            _hostWindow = window;
            _hostClientSize = window.ClientSize;
            _isExternallyHosted = true;

            _hostTexture = RHIDevice.Instance!.CreateTexture(new RHITextureDescription
            {
                Width = (int)window.ClientSize.X,
                Height = (int)window.ClientSize.Y,
                DepthOrArraySize = 1,
                
                MipLevels = 1,

                Dimension = RHIDimension.Texture2D,
                Format = RHIFormat.RGB10A2_UNorm,
                Usage = RHIResourceUsage.ShaderResource | RHIResourceUsage.RenderTarget,

                Swizzle = RHISwizzle.RGBA
            }, Span<nint>.Empty, "UIHost-Backing");

            _invalidationFlags = UIInvalidationFlags.All;
        }

        internal void RemoveInvalidFlags(UIInvalidationFlags flags)
        {
            _invalidationFlags &= ~flags;
        }

        internal void TryChangeWindowSize(UIWindow window, Vector2 newClientSize)
        {
            if (_tabbedWindows.Count == 1 && _dockedHosts.Count == 0 && !_isExternallyHosted)
            {
                _hostWindow?.ClientSize = newClientSize;
                _invalidationFlags |= UIInvalidationFlags.All;
            }
        }

        internal void RecalculateLayout()
        {
            Boundaries bounds = new Boundaries(Vector2.Zero, _hostClientSize);
            for (int i = 0; i < _dockedHosts.Count; i++)
            {
                UIDockHost dockHost = _dockedHosts[i];
                dockHost._invalidationFlags |= UIInvalidationFlags.All;

                switch (dockHost._dockedSide)
                {
                    case UIDockSide.Left:
                        {
                            dockHost._hostClientSize.Y = bounds.Maximum.Y - bounds.Minimum.Y;
                            bounds.Minimum.X += dockHost._hostClientSize.X;
                            break;
                        }
                    case UIDockSide.Right:
                        {
                            dockHost._hostClientSize.Y = bounds.Maximum.Y - bounds.Minimum.Y;
                            bounds.Maximum.X -= dockHost._hostClientSize.X;
                            break;
                        }
                    case UIDockSide.Top:
                        {
                            dockHost._hostClientSize.X = bounds.Maximum.X - bounds.Minimum.X;
                            bounds.Minimum.Y += dockHost._hostClientSize.Y;
                            break;
                        }
                    case UIDockSide.Bottom:
                        {
                            dockHost._hostClientSize.X = bounds.Maximum.X - bounds.Minimum.X;
                            bounds.Maximum.Y -= dockHost._hostClientSize.Y;
                            break;
                        }
                }
            }

            _tabbedClientSize = bounds;

            Vector2 clientSize = bounds.Size;
            for (int i = 0; i < _tabbedWindows.Count; i++)
            {
                _tabbedWindows[i].SetClientSizeFromHost(clientSize);
            }
        }

        public void DockNewWindow(UIWindow window)
        {
            window.ParentHost?.UndockWindow(window);
            window.ParentHost = this;

            _tabbedWindows.Add(window);
            _invalidationFlags |= UIInvalidationFlags.All;
        }

        public void DockNewHost(UIDockHost host, UIDockSide side)
        {
            host.ParentHost?.UndockHost(host);

            host.ParentHost = this;
            host.DockedSide = side;

            _dockedHosts.Add(host);
            _invalidationFlags |= UIInvalidationFlags.All;
        }

        private void UndockWindow(UIWindow window)
        {
            if (!_tabbedWindows.Remove(window))
                UIManager.Logger?.Warning("Cannot remove already undocked window: \"{wnd}\" (id: {id})", window.WindowTitle, window.UniqueWindowId);
            
            _invalidationFlags |= UIInvalidationFlags.All;
        }

        private void UndockHost(UIDockHost host)
        {
            if (!_dockedHosts.Remove(host))
                UIManager.Logger?.Warning("Cannot remove already undocked host");
            
            _invalidationFlags |= UIInvalidationFlags.All;
        }

        public UIDockHost? ParentHost { get => _parentHost; internal set => _parentHost = value; }
        public UIDockSide DockedSide { get => _dockedSide; internal set => _dockedSide = value; }

        public Window? Window => _hostWindow;
        public Vector2 ClientSize => _hostClientSize;
        public bool IsExternallyHosted => _isExternallyHosted;

        public RHITexture? HostTexture => _hostTexture;

        public IReadOnlyList<UIWindow> TabbedWindows => _tabbedWindows;
        public IReadOnlyList<UIDockHost> DockedHosts => _dockedHosts;

        public UIInvalidationFlags InvalidationFlags => _invalidationFlags;

        public UIWindow? ActiveWindow => _activeTabbedWindow < _tabbedWindows.Count ? _tabbedWindows[_activeTabbedWindow] : null;
    }

    public enum UIDockSide : byte
    {
        Left = 0,
        Right,
        Top,
        Bottom,
    }
}
