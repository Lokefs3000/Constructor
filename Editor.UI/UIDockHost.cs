using CommunityToolkit.HighPerformance;
using Editor.UI.Assets;
using Editor.UI.Elements;
using Editor.UI.Helpers;
using Editor.UI.Interaction;
using Editor.UI.Text;
using Editor.UI.Visual;
using Primary;
using Primary.Assets;
using Primary.Common;
using Primary.Mathematics;
using Primary.Memory.Native;
using Primary.RHI;
using Primary.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI
{
    public sealed class UIDockHost : IWindowDockHost, IInteractable, IDisposable
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

        private List<IWindow> _tabbedWindows;
        private Rect _tabbedClientSize;
        private int _activeTabbedWindow;

        private List<UIDockHost> _dockedHosts;

        private UIStateFlags _stateFlags;

        private UIFontAsset? _font;

        private bool _disposedValue;

        public UIDockHost(int uniqueId)
        {
            _uniqueDockHostId = uniqueId;

            _parentHost = null;
            _dockedSide = UIDockSide.Left;

            _interactionManager = new HostInteractionManager();

            _hostWindow = null;
            _hostClientOffset = Int2.Zero;
            _hostClientSize = Int2.Zero;
            _isExternallyHosted = false;

            _hostTexture = null;

            _tabbedWindows = new List<IWindow>();
            _tabbedClientSize = Rect.Zero;
            _activeTabbedWindow = 0;

            _dockedHosts = new List<UIDockHost>();

            _stateFlags = UIStateFlags.None;

            _font = AssetManager.LoadAsset<UIFontAsset>("Editor/Fonts/Inter.uifont");
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _hostTexture?.Dispose();
                    _hostTexture = null;

                    if (!_isExternallyHosted && _hostWindow != null)
                        WindowManager.Instance.DestroyWindow(_hostWindow);
                    _hostWindow = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
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
            }, [], $"UIHost-{_uniqueDockHostId}");

            _stateFlags = UIStateFlags.InvalidAll;

            window.WindowResized += WindowResizedCallback;
        }

        private void WindowResizedCallback(Int2 newSize)
        {
            _hostClientSize = newSize;
            _stateFlags |= UIStateFlags.InvalidAll;

            foreach (IWindow window in _tabbedWindows)
            {
                window.InvalidateTree(UIStateFlags.InvalidLayout);
            }

            _hostTexture?.Dispose();
            _hostTexture = null;
        }

        public void Update()
        {
            ActiveWindow?.Update();
        }

        public void TryChangeWindowSize(IWindow window, Int2 newClientSize)
        {
            if (_tabbedWindows.Count == 1 && _dockedHosts.Count == 0 && !_isExternallyHosted)
            {
                _hostWindow?.ClientSize = newClientSize;
                _stateFlags |= UIStateFlags.InvalidAll;
            }
        }

        private void TryChangeTabbedClientSize(Rect newClientSize)
        {
            if (_tabbedClientSize != newClientSize)
            {
                Int2 size = newClientSize.Size;

                _hostTexture?.Dispose();
                _hostTexture = RHIDevice.Instance!.CreateTexture(new RHITextureDescription
                {
                    Width = size.X,
                    Height = size.Y,
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
            RemoveStateFlags(UIStateFlags.InvalidLayout);

            Boundaries bounds = new Boundaries(_hostClientOffset.AsVector2(), (_hostClientOffset + _hostClientSize).AsVector2());
            for (int i = 0; i < _dockedHosts.Count; i++)
            {
                UIDockHost dockHost = _dockedHosts[i];

                dockHost._stateFlags |= UIStateFlags.InvalidAll;
                dockHost._hostClientSize = Int2.Max(dockHost._hostClientSize, new Int2(16));

                Rect hostRect = dockHost.HostMetrics;

                switch (dockHost._dockedSide)
                {
                    case UIDockSide.Left:
                        {
                            dockHost._hostClientSize = new Int2(dockHost._hostClientSize.X, (int)(bounds.Maximum.Y - bounds.Minimum.Y));
                            dockHost._hostClientOffset = _hostClientOffset + bounds.Minimum.AsInt2();

                            bounds.Minimum.X += dockHost._hostClientSize.X;
                            break;
                        }
                    case UIDockSide.Right:
                        {
                            dockHost._hostClientSize = new Int2(dockHost._hostClientSize.X, (int)(bounds.Maximum.Y - bounds.Minimum.Y));
                            dockHost._hostClientOffset = _hostClientOffset + new Vector2(bounds.Maximum.X - dockHost._hostClientSize.X, bounds.Minimum.Y).AsInt2();

                            bounds.Maximum.X -= dockHost._hostClientSize.X;
                            break;
                        }
                    case UIDockSide.Top:
                        {
                            dockHost._hostClientSize = new Int2((int)(bounds.Maximum.X - bounds.Minimum.X), dockHost._hostClientSize.Y);
                            dockHost._hostClientOffset = _hostClientOffset + bounds.Minimum.AsInt2();

                            bounds.Minimum.Y += dockHost._hostClientSize.Y;
                            break;
                        }
                    case UIDockSide.Bottom:
                        {
                            dockHost._hostClientSize = new Int2((int)(bounds.Maximum.X - bounds.Minimum.X), dockHost._hostClientSize.Y);
                            dockHost._hostClientOffset = _hostClientOffset + new Vector2(bounds.Minimum.X, bounds.Maximum.Y - dockHost._hostClientSize.Y).AsInt2();

                            bounds.Maximum.Y -= dockHost._hostClientSize.Y;
                            break;
                        }
                }

                if (hostRect != dockHost.HostMetrics)
                {
                    foreach (IWindow window in dockHost.TabbedWindows)
                    {
                        window.InvalidateTree(UIStateFlags.InvalidLayout);
                    }
                }
            }

            TryChangeTabbedClientSize(bounds.AsRect());

            Int2 clientSize = bounds.Size.AsInt2();
            for (int i = 0; i < _tabbedWindows.Count; i++)
            {
                _tabbedWindows[i].SetClientSizeFromHost(clientSize);
            }
        }

        public void DrawVisual(UIPainterContext painter)
        {
            if (_font == null || !_font.IsLoaded)
                return;

            UIFontTypeData typeData = _font.FindStyle(FontWeight.Lighter)!;

            Boundaries bounds = new Boundaries(Vector2.Zero, new Vector2(_tabbedClientSize.Size.X, 26.0f));

            TextManager textManager = UIManager.Instance.TextManager;

            PaintColor color = new PaintColor(Color.White);
            TextVisualInfo visualInfo = new TextVisualInfo(color, 0.75f, typeData);
            TextWrapInfo wrapInfo = new TextWrapInfo(TextOrigin.Top, Vector2.PositiveInfinity, false, visualInfo);

            painter.PushClippingRect(bounds);

            painter.DrawRect(bounds, UIPaint.FromColor(s_tabBarBackgroundColor));

            float offsetX = bounds.Minimum.X;

            TextBuilder builder = new TextBuilder();
            builder.SetAllowRichText(false);
            builder.SetAlignment(UITextAlignment.MiddleCenter);

            for (int i = 0; i < _tabbedWindows.Count; ++i)
            {
                IWindow window = _tabbedWindows[i];

                string windowTitleStr = window is UIWindow uiWindow ? uiWindow.WindowTitle : string.Empty;

                ReadOnlySpan<char> windowTitle = windowTitleStr.AsSpan()[..Math.Min(windowTitleStr.Length, 15)];
                StringHandle stringHandle = painter.Painter.GetStringHandle(windowTitle);

                ShapedTextData textData = textManager.ShapeText(wrapInfo, UITextOverflow.Overflow, stringHandle.String, stringHandle.Hash);

                Boundaries tabBounds = new Boundaries(new Vector2(offsetX, bounds.Minimum.Y + 1.0f), new Vector2(offsetX + textData.TotalSize.X + 8.0f, bounds.Maximum.Y + 4.0f));

                if (i == _activeTabbedWindow)
                {
                    UIPaint paint = new UIPaint();
                    paint.SetColor(s_tabActiveBackgroundColor);
                    paint.SetStroke(true);
                    paint.SetStrokeWidth(1.0f);
                    paint.SetStrokeColor(s_tabActiveBorderColor);

                    painter.DrawRoundedRect(tabBounds, paint, 8.0f);
                }

                builder.SetMaxExtents(tabBounds.Size - new Vector2(0.0f, 10.0f));
                painter.DrawText(new Vector2(tabBounds.Minimum.X, tabBounds.Minimum.Y + 1.0f), UIPaint.FromColor(Color.White), builder, typeData, 0.75f, windowTitle);

                offsetX += float.Truncate(textData.TotalSize.X) + 8.0f;
            }

            painter.PopClippingRect();
        }

        public void HandleEvent(ref readonly UIEvent @event)
        {
            switch (@event.Type)
            {
                case UIEventType.MouseActivate:
                    {
                        if (@event.Mouse.Position.Y > 26.0f)
                            return;

                        IWindow? tabWindow = GetTabWindowAt(@event.Mouse.Position.X);
                        if (tabWindow != null)
                        {
                            ChangeActiveWindowIndex(_tabbedWindows.IndexOf(tabWindow));
                            UIManager.Instance.WindowManager.FocusWindow(_tabbedWindows[_activeTabbedWindow]);
                        }

                        break;
                    }
                case UIEventType.MouseWheel:
                    {
                        if (@event.Mouse.Position.Y > 26.0f)
                            return;

                        ChangeActiveWindowIndex(Math.Clamp(_activeTabbedWindow + (int)@event.Mouse.Delta.Y, 0, _tabbedWindows.Count - 1));
                        break;
                    }
            }
        }

        private IWindow? GetTabWindowAt(float x)
        {
            if (_font == null || !_font.IsLoaded)
                return null;

            UIFontTypeData typeData = _font.FindStyle(FontWeight.Lighter)!;

            TextManager textManager = UIManager.Instance.TextManager;

            TextVisualInfo visualInfo = new TextVisualInfo(default, 0.75f, typeData);
            TextWrapInfo wrapInfo = new TextWrapInfo(TextOrigin.Top, Vector2.PositiveInfinity, false, visualInfo);

            float offsetX = 0.0f;

            Span<char> tempStr = stackalloc char[16];

            for (int i = 0; i < _tabbedWindows.Count; ++i)
            {
                IWindow window = _tabbedWindows[i];
                string windowTitleStr = window is UIWindow uiWindow ? uiWindow.WindowTitle : string.Empty;

                ReadOnlySpan<char> windowTitle = windowTitleStr.AsSpan()[..Math.Min(windowTitleStr.Length, 15)];

                windowTitle.CopyTo(tempStr);
                tempStr[windowTitle.Length] = '\0';

                ShapedTextData textData = textManager.ShapeText(wrapInfo, UITextOverflow.Overflow, tempStr[..(windowTitle.Length + 1)], windowTitle.GetDjb2HashCode());
                offsetX += float.Truncate(textData.TotalSize.X) + 8.0f;

                if (offsetX >= x)
                    return window;
            }

            return null;
        }

        public void DockNewWindow(IWindow window)
        {
            if (window.ParentHost is IWindowDockHost prevDockHost)
                prevDockHost.UndockWindow(window);
            window.SetWindowHost(this);

            _tabbedWindows.Add(window);
            _stateFlags |= UIStateFlags.InvalidAll;

            UIManager.Instance.WindowManager.FocusWindow(window);
            ChangeActiveWindowIndex(_tabbedWindows.Count - 1);
        }

        public void DockNewHost(UIDockHost host, UIDockSide side)
        {
            host.ParentHost?.UndockHost(host);

            host.ParentHost = this;
            host.DockedSide = side;

            _dockedHosts.Add(host);
            _stateFlags |= UIStateFlags.InvalidAll;
        }

        public void FocusWindow(IWindow window)
        {
            int index = _tabbedWindows.IndexOf(window);
            if (index != -1)
            {
                ChangeActiveWindowIndex(index);
                UIManager.Instance.WindowManager.FocusWindow(window);
            }
        }

        public void UndockWindow(IWindow window)
        {
            int index = _tabbedWindows.IndexOf(window);
            if (index == -1)
                UIManager.Logger?.Warning("Cannot remove already undocked window: \"{wnd}\"", window);
            else
            {
                if (_activeTabbedWindow == index)
                    ChangeActiveWindowIndex(Math.Min(_activeTabbedWindow, _tabbedWindows.Count - 1));
                else if (_activeTabbedWindow > index)
                    --_activeTabbedWindow;

                _tabbedWindows.RemoveAt(index);
            }

            _stateFlags |= UIStateFlags.InvalidAll;
        }

        private void UndockHost(UIDockHost host)
        {
            if (!_dockedHosts.Remove(host))
                UIManager.Logger?.Warning("Cannot remove already undocked host");

            _stateFlags |= UIStateFlags.InvalidAll;
        }

        private void ChangeActiveWindowIndex(int newIndex)
        {
            if (_activeTabbedWindow == newIndex)
                return;

            if (_activeTabbedWindow != -1)
                _tabbedWindows[_activeTabbedWindow]?.OnHiddenCallback();
            if (newIndex != -1)
                _tabbedWindows[newIndex]?.OnShownCallbacks();

            _activeTabbedWindow = newIndex;
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

        public void RemoveStateFlags(UIStateFlags flags)
        {
            _stateFlags &= ~flags;
        }

        public IInteractable GetInteractable(Vector2 point)
        {
            if (point.Y <= 26.0f)
                return this;
            return ((IInteractable?)ActiveWindow?.RootElement) ?? this;
        }

        internal Int2 GetDisplayPosition(Int2 position)
        {
            position += _hostClientOffset + _tabbedClientSize.Position;

            if (_hostWindow != null)
                position += _hostWindow.Position;
            else
                position = _parentHost?.GetDisplayPosition(position) ?? position;

            return position;
        }

        public int UniqueDockHostId => _uniqueDockHostId;

        public UIDockHost? ParentHost { get => _parentHost; internal set => _parentHost = value; }
        public UIDockSide DockedSide { get => _dockedSide; internal set => _dockedSide = value; }

        public HostInteractionManager InteractionManager => _interactionManager;

        IWindowDockHost? IWindowDockHost.ParentHost => _parentHost;

        public Window? Window => _hostWindow;
        public bool IsExternallyHosted => _isExternallyHosted;

        public Rect HostMetrics => new Rect(_hostClientOffset, _hostClientSize);
        public Rect ContentMetrics => _tabbedClientSize;

        public Boundaries InvalidVisualRegion => ActiveWindow?.InvalidVisualRegion ?? Boundaries.Zero;

        public RHITexture? HostTexture => _hostTexture;
        public Window? HostWindow => _hostWindow;

        public IReadOnlyList<IWindow> TabbedWindows => _tabbedWindows;
        public IReadOnlyList<UIDockHost> DockedHosts => _dockedHosts;

        public UIStateFlags InvalidationFlags => _stateFlags;
        public IWindow? ActiveWindow => _activeTabbedWindow < _tabbedWindows.Count ? _tabbedWindows[_activeTabbedWindow] : null;

        ReadOnlySpan<IWindow> IWindowDockHost.Windows => _tabbedWindows.AsSpan();
        ReadOnlySpan<IWindowHost> IWindowDockHost.Hosts => _dockedHosts.AsSpan();

        IWindowHost? IInteractable.Host => this;
        public IInteractionShape? Shape => null;

        private static readonly Color s_tabBarBackgroundColor = Color.FromHex("151515");

        private static readonly Color s_tabActiveBackgroundColor = Color.FromHex("242424");
        private static readonly Color s_tabActiveBorderColor = Color.FromHex("101010");
    }

    public enum UIDockSide : byte
    {
        Left = 0,
        Right,
        Top,
        Bottom,
    }
}
