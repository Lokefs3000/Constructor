using CommunityToolkit.HighPerformance;
using Editor.UI.Interaction;
using Primary;
using Primary.Common;
using Primary.Mathematics;
using Primary.Memory.Native;
using Primary.Rendering;
using Primary.RHI2;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI
{
    public sealed class UIDockHost : IWindowHost
    {
        private readonly int _uniqueDockHostId;

        private UIDockHost? _parentHost;
        private UIDockSide _dockedSide;

        private HostInteractionManager _interactionManager;

        private Window? _hostWindow;
        private Int2 _hostClientOffset;
        private Int2 _hostClientSize;
        private bool _isExternallyHosted;

        private RHITexture? _hostTexture;

        private List<UIWindow> _tabbedWindows;
        private Boundaries _tabbedClientSize;
        private int _activeTabbedWindow;

        private List<UIDockHost> _dockedHosts;

        private UIStateFlags _stateFlags;

        public UIDockHost(int uniqueId)
        {
            _uniqueDockHostId = uniqueId;

            _parentHost = null;
            _dockedSide = UIDockSide.Left;

            _interactionManager = new HostInteractionManager(this);

            _hostWindow = null;
            _hostClientOffset = Int2.Zero;
            _hostClientSize = Int2.Zero;
            _isExternallyHosted = false;

            _hostTexture = null;

            _tabbedWindows = new List<UIWindow>();
            _tabbedClientSize = Boundaries.Zero;
            _activeTabbedWindow = 0;

            _dockedHosts = new List<UIDockHost>();

            _stateFlags = UIStateFlags.None;
        }

        internal void SetupAsFloating(Int2 clientSize)
        {
            _hostWindow = Engine.GlobalSingleton.WindowManager.CreateWindow("Empty dock host", clientSize, CreateWindowFlags.Resizable);
            _hostClientSize = clientSize;

            _stateFlags = UIStateFlags.InvalidAll;
        }

        internal void SetupAsDocked(UIDockHost host, UIDockSide side)
        {
            host.DockNewHost(this, side);

            _stateFlags = UIStateFlags.InvalidAll;
        }

        internal void SetupAsHosted(Window window)
        {
            _hostWindow = window;
            _hostClientSize = window.ClientSize;
            _isExternallyHosted = true;

            _hostTexture = RHIDevice.Instance!.CreateTexture(new RHITextureDescription
            {
                Width = window.ClientSize.X,
                Height = window.ClientSize.Y,
                DepthOrArraySize = 1,
                
                MipLevels = 1,

                Dimension = RHIDimension.Texture2D,
                Format = RHIFormat.RGB10A2_UNorm,
                Usage = RHIResourceUsage.ShaderResource | RHIResourceUsage.RenderTarget,

                Swizzle = RHISwizzle.RGBA
            }, Span<ArrayPtr<byte>>.Empty, "UIHost-Backing");

            _stateFlags = UIStateFlags.InvalidAll;
        }

        public void TryChangeWindowSize(UIWindow window, Int2 newClientSize)
        {
            if (_tabbedWindows.Count == 1 && _dockedHosts.Count == 0 && !_isExternallyHosted)
            {
                _hostWindow?.ClientSize = newClientSize;
                _stateFlags |= UIStateFlags.InvalidAll;
            }
        }

        private void TryChangeTabbedClientSize(Boundaries newClientSize)
        {
            if (_tabbedClientSize != newClientSize)
            {
                Vector2 size = newClientSize.Size;

                _hostTexture?.Dispose();
                _hostTexture = RHIDevice.Instance!.CreateTexture(new RHITextureDescription
                {
                    Width = (int)size.X,
                    Height = (int)size.Y,
                    DepthOrArraySize = 1,

                    MipLevels = 1,

                    Dimension = RHIDimension.Texture2D,
                    Format = RHIFormat.RGB10A2_UNorm,
                    Usage = RHIResourceUsage.ShaderResource | RHIResourceUsage.RenderTarget,

                    Swizzle = RHISwizzle.RGBA
                }, Span<ArrayPtr<byte>>.Empty, "UIHost-Backing");

                _tabbedClientSize = newClientSize;

                AddStateFlags(UIStateFlags.InvalidAll);
            }
        }

        public void RecalculateLayout()
        {
            Boundaries bounds = new Boundaries(Vector2.Zero, _hostClientSize.AsVector2());
            for (int i = 0; i < _dockedHosts.Count; i++)
            {
                UIDockHost dockHost = _dockedHosts[i];

                dockHost._stateFlags |= UIStateFlags.InvalidAll;
                dockHost._hostClientSize = Int2.Max(dockHost._hostClientSize, new Int2(16));

                switch (dockHost._dockedSide)
                {
                    case UIDockSide.Left:
                        {
                            dockHost._hostClientSize = new Int2(dockHost._hostClientSize.X, (int)(bounds.Maximum.Y - bounds.Minimum.Y));
                            bounds.Minimum.X += dockHost._hostClientSize.X;

                            dockHost._hostClientOffset = _hostClientOffset + bounds.Minimum.AsInt2();
                            break;
                        }
                    case UIDockSide.Right:
                        {
                            dockHost._hostClientSize = new Int2(dockHost._hostClientSize.X, (int)(bounds.Maximum.Y - bounds.Minimum.Y));
                            bounds.Maximum.X -= dockHost._hostClientSize.X;
                            
                            dockHost._hostClientOffset = _hostClientOffset + new Vector2(bounds.Maximum.X, bounds.Minimum.Y).AsInt2();
                            break;
                        }
                    case UIDockSide.Top:
                        {
                            dockHost._hostClientSize = new Int2((int)(bounds.Maximum.X - bounds.Minimum.X), dockHost._hostClientSize.Y);
                            bounds.Minimum.Y += dockHost._hostClientSize.Y;
                            
                            dockHost._hostClientOffset = _hostClientOffset + bounds.Minimum.AsInt2();
                            break;
                        }
                    case UIDockSide.Bottom:
                        {
                            dockHost._hostClientSize = new Int2((int)(bounds.Maximum.X - bounds.Minimum.X), dockHost._hostClientSize.Y);
                            bounds.Maximum.Y -= dockHost._hostClientSize.Y;

                            dockHost._hostClientOffset = _hostClientOffset + new Vector2(bounds.Minimum.X, bounds.Maximum.Y).AsInt2();
                            break;
                        }
                }
            }

            TryChangeTabbedClientSize(bounds);

            Int2 clientSize = bounds.Size.AsInt2();
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
            _stateFlags |= UIStateFlags.InvalidAll;
        }

        public void DockNewHost(UIDockHost host, UIDockSide side)
        {
            host.ParentHost?.UndockHost(host);

            host.ParentHost = this;
            host.DockedSide = side;

            _dockedHosts.Add(host);
            _stateFlags |= UIStateFlags.InvalidAll;
        }

        public void UndockWindow(UIWindow window)
        {
            if (!_tabbedWindows.Remove(window))
                UIManager.Logger?.Warning("Cannot remove already undocked window: \"{wnd}\" (id: {id})", window.WindowTitle, window.UniqueWindowId);
            
            _stateFlags |= UIStateFlags.InvalidAll;
        }

        private void UndockHost(UIDockHost host)
        {
            if (!_dockedHosts.Remove(host))
                UIManager.Logger?.Warning("Cannot remove already undocked host");
            
            _stateFlags |= UIStateFlags.InvalidAll;
        }

        public void SetHostSize(int size)
        {
            if (_parentHost == null)
                return;

            size = Math.Max(size, 16);

            switch (_dockedSide)
            {
                case UIDockSide.Left:
                case UIDockSide.Right: _hostClientSize.X = size; break;
                case UIDockSide.Top:
                case UIDockSide.Bottom: _hostClientSize.Y = size; break;
            }

            AddStateFlags(UIStateFlags.InvalidLayout);
        }

        public void AddStateFlags(UIStateFlags flags)
        {
            _stateFlags |= flags;
            _parentHost?.AddStateFlags(flags);
        }

        internal void RemoveStateFlags(UIStateFlags flags)
        {
            _stateFlags &= ~flags;
        }

        public int UniqueDockHostId => _uniqueDockHostId;

        public UIDockHost? ParentHost { get => _parentHost; internal set => _parentHost = value; }
        public UIDockSide DockedSide { get => _dockedSide; internal set => _dockedSide = value; }

        public HostInteractionManager InteractionManager => _interactionManager;

        public Window? Window => _hostWindow;
        public Int2 ClientSize => _hostClientSize;
        public Int2 ClientOffset => _hostClientOffset;
        public bool IsExternallyHosted => _isExternallyHosted;

        public RHITexture? HostTexture => _hostTexture;

        public IReadOnlyList<UIWindow> TabbedWindows => _tabbedWindows;
        public IReadOnlyList<UIDockHost> DockedHosts => _dockedHosts;

        public Boundaries TabbedClientBounds => _tabbedClientSize;

        public UIStateFlags InvalidationFlags => _stateFlags;

        public UIWindow? ActiveWindow => _activeTabbedWindow < _tabbedWindows.Count ? _tabbedWindows[_activeTabbedWindow] : null;

        public ReadOnlySpan<UIWindow> Windows => _tabbedWindows.AsSpan();
    }

    public enum UIDockSide : byte
    {
        Left = 0,
        Right,
        Top,
        Bottom,
    }
}
