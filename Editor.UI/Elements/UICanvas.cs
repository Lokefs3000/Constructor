using CommunityToolkit.Diagnostics;
using Editor.UI.Interaction;
using Editor.UI.Layout;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Mathematics;
using Primary.Memory.Native;
using Primary.RHI;
using Primary.Windowing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("Canvas")]
    public class UICanvas : UIFrame
    {
        private Vector2 _clientOffset;
        private Int2 _clientSize;

        private CanvasWindowHost _host;

        public UICanvas()
        {
            _host = new CanvasWindowHost(this);
        }

        public UICanvas(UIElement parent) : this()
        {
            SetParent(parent);
        }

        public override void RecalculateLayout(UILayoutContext context)
        {
            if (Flags.HasFlag(StateFlags, (UIStateFlags)UIStateFlagsExt.InvalidHost))
            {
                _host.AddStateFlags(UIStateFlags.InvalidLayout);
                RemoveStateFlags((UIStateFlags)UIStateFlagsExt.InvalidHost);
            }

            base.RecalculateLayout(context);
        }

        public override bool DrawVisual(UIPainterContext painter)
        {
            base.DrawVisual(painter);

            if (_host.HostedWindow != null && _host.HostTexture != null)
            {
                Vector2 baseOffset = _clientOffset + _viewCoordinates.Minimum;
                Boundaries boundaries = new Boundaries(baseOffset, baseOffset + _clientSize.AsVector2() / _host.HostedWindow.ClientSize.AsVector2() * _clientSize.AsVector2());

                painter.DrawImage(boundaries, UIPaint.FromColor(Color.White), _host.HostTexture);
            }

            return true;
        }

        public UIWindow? HostedWindow
        {
            get => _host.HostedWindow;
            set
            {
                if (value != _host.HostedWindow)
                {
                    if (value == null)
                    {
                        _host.UndockWindow(_host.HostedWindow!);
                    }
                    else
                    {
                        _host.DockNewWindow(value);
                    }
                }
            }
        }

        #region Properties
        [EditableProperty(nameof(_clientOffset), (UIStateFlags)UIStateFlagsExt.InvalidHost)] public Vector2 ClientOffset { get => _clientOffset; set => SetEditableProperty(value); }
        [EditableProperty(nameof(_clientSize), (UIStateFlags)UIStateFlagsExt.InvalidHost)] public Int2 ClientSize { get => _clientSize; set => SetEditableProperty(value); }
        #endregion

        private enum UIStateFlagsExt : byte
        {
            InvalidHost = 1 << 4
        }

        private sealed class CanvasWindowHost : IWindowHost
        {
            private readonly UICanvas _canvas;
            private RHITexture? _texture;

            private UIWindow? _hostedWindow;

            private UIStateFlags _stateFlags;

            internal CanvasWindowHost(UICanvas canvas)
            {
                _canvas = canvas;
                _texture = null;

                _hostedWindow = null;

                _stateFlags = UIStateFlags.None;
            }

            public void DockNewWindow(UIWindow window)
            {
                Guard.IsNull(_hostedWindow);

                

                _hostedWindow = window;
                AddStateFlags(UIStateFlags.InvalidAll);
            }

            public void UndockWindow(UIWindow window)
            {
                Guard.IsNotNull(_hostedWindow);
                Guard.Equals(_hostedWindow, window);

                
                _hostedWindow = null;

                AddStateFlags(UIStateFlags.InvalidAll);
            }

            public void FocusWindow(UIWindow window)
            {
            }

            public void RecalculateLayout()
            {
                UIManager.Instance.Renderer.AddHostToRedrawQueue(this);
            }

            public void DrawVisual(UIPainterContext context)
            {

            }

            public void TryChangeWindowSize(UIWindow window, Int2 newClientSize)
            {
                window.SetClientSizeFromHost(newClientSize);
            }

            public void AddStateFlags(UIStateFlags flags)
            {
                _stateFlags |= flags;

                if (Flags.HasFlag(flags, UIStateFlags.InvalidLayout))
                    UIManager.Instance.LayoutManager.AddInvalidLayout(this);
                else if (Flags.HasFlag(flags, UIStateFlags.InvalidVisual))
                    UIManager.Instance.Renderer.AddHostToRedrawQueue(this);
            }

            private void CreateNewTexture(Int2 size)
            {
                _texture?.Dispose();
                _texture = RHIDevice.Instance?.CreateTexture(new RHITextureDescription
                {
                    Width = size.X,
                    Height = size.Y,

                    MipLevels = 1,

                    Usage = RHIResourceUsage.RenderTarget | RHIResourceUsage.ShaderResource,
                    Dimension = RHIDimension.Texture2D,
                    Format = RHIFormat.RGB10A2_UNorm,
                }, Span<ArrayPtr<byte>>.Empty, "CanvasHostTexture");
            }

            public void TryChangeWindowSize(IWindow window, Int2 newClientSize)
            {
                throw new NotImplementedException();
            }

            public IInteractable GetInteractable(Vector2 point)
            {
                throw new NotImplementedException();
            }

            public void HandleEvent(ref readonly UIEvent @event)
            {
                throw new NotImplementedException();
            }

            public void Update()
            {
                throw new NotImplementedException();
            }

            public void RemoveStateFlags(UIStateFlags flags)
            {
                throw new NotImplementedException();
            }

            internal UIWindow? HostedWindow => _hostedWindow;

            public IWindowHost? ParentHost => null;

            public Int2 ClientOffset => _canvas._clientOffset.AsInt2();
            public Int2 ClientSize => _canvas._currentSize.AsInt2();

            public Boundaries WindowClientSize => new Boundaries(Vector2.Zero, _canvas._currentSize);

            public HostInteractionManager InteractionManager => throw new NotImplementedException();

            public ReadOnlySpan<UIWindow> Windows => _hostedWindow == null ? ReadOnlySpan<UIWindow>.Empty : new ReadOnlySpan<UIWindow>(ref _hostedWindow);
            public UIWindow? ActiveWindow => _hostedWindow;

            public RHITexture? HostTexture
            {
                get
                {
                    if (_hostedWindow == null)
                    {
                        if (_texture != null)
                        {
                            _texture.Dispose();
                            _texture = null;
                        }

                        return null;
                    }

                    Int2 windowSize = _hostedWindow.ClientSize;
                    if (_texture == null)
                    {
                        if (Math.Min(windowSize.X, windowSize.Y) < 1)
                            return null;

                        CreateNewTexture(windowSize);
                        return _texture;
                    }
                    else
                    {
                        if (Math.Min(windowSize.X, windowSize.Y) < 1)
                        {
                            _texture.Dispose();
                            _texture = null;

                            return null;
                        }

                        if (windowSize != new Int2(_texture.Description.Width, _texture.Description.Height))
                            CreateNewTexture(windowSize);
                        return _texture;
                    }
                }
            }

            public Window? HostWindow => null;

            IWindow? IWindowHost.ActiveWindow => ActiveWindow;

            public UIStateFlags InvalidationFlags => throw new NotImplementedException();

            public IInteractionShape? Shape => throw new NotImplementedException();

            public Rect HostMetrics => throw new NotImplementedException();

            public Rect ContentMetrics => throw new NotImplementedException();

            public Boundaries InvalidVisualRegion => throw new NotImplementedException();

            public IWindowHost? Host => throw new NotImplementedException();
        }
    }
}
