using Editor.UI.Assets;
using Editor.UI.Elements;
using Editor.UI.Interaction;
using Editor.UI.Serialization;
using Editor.UI.Styling;
using Editor.UI.Visual;
using Primary;
using Primary.Common;
using Primary.Mathematics;
using Primary.Memory.Native;
using Primary.RHI;
using Primary.Windowing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Editor.UI.Popup
{
    public abstract class PopupHost : IDisposable, IWindowHost, IWindow, IRenderableWindow
    {
        private readonly Window _parentWindow;
        private readonly Window _owningWindow;
        private RHITexture? _rhiTexture;

        private readonly HostInteractionManager _interactionManager;

        private readonly StyleProvider _styleProvider;
        private readonly StyleUpdater _styleUpdater;

        private bool _isStyleFullyInvalid;

        private Int2 _originPosition;
        private Int2 _clientSize;

        private UIStateFlags _stateFlags;

        private bool _disposedValue;

        public PopupHost(Window parentWindow, Window owningWindow, Int2 originPosition)
        {
            _parentWindow = parentWindow;
            _owningWindow = owningWindow;
            _rhiTexture = null;

            _interactionManager = new HostInteractionManager();

            _isStyleFullyInvalid = false;

            _styleProvider = new StyleProvider();
            _styleUpdater = new StyleUpdater();

            _originPosition = originPosition;
            _clientSize = Int2.One;

            _stateFlags = UIStateFlags.InvalidAll;

            owningWindow.OnFocusGained += OnFocusGainedCallback;
            owningWindow.OnFocusLost += OnFocusLostCallback;
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    CleanupSelf();

                    _owningWindow.OnFocusGained -= OnFocusGainedCallback;
                    _owningWindow.OnFocusLost -= OnFocusLostCallback;

                    _rhiTexture?.Dispose();
                    _rhiTexture = null;
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected abstract void CleanupSelf();
        protected abstract Int2 GetContentSize();

        private void OnFocusGainedCallback(Window window)
        {
            PopupManager popup = UIManager.Instance.PopupManager;
            popup.FocusPopup(this);
        }

        private void OnFocusLostCallback(Window window)
        {
            PopupManager popup = UIManager.Instance.PopupManager;
            popup.BlurPopup(this);
        }

        public virtual void Update()
        {
            if (_isStyleFullyInvalid)
            {
                RefreshTreeStyling();
                _isStyleFullyInvalid = false;
            }
        }

        public void Close()
        {
            PopupManager popup = UIManager.Instance.PopupManager;
            popup.ClosePopup(this);
        }

        void IWindow.SetWindowHost(IWindowHost? host)
        {
            throw new NotSupportedException("A PopupHost cannot be owned by any host other then itself");
        }

        void IWindow.SetClientSizeFromHost(Int2 clientSize)
        {
            throw new UnreachableException("This should not be called! Something has gone wrong behind the scenes");
        }

        public void InvalidateTree(UIStateFlags flags)
        {
            AddStateFlags(flags);
            RecursiveInvalidate(RootElement);

            void RecursiveInvalidate(UIElement element)
            {
                element.AddStateFlags(flags);

                foreach (UIElement child in element.Children)
                {
                    RecursiveInvalidate(child);
                }
            }
        }

        public T? FindElementWithId<T>(string id) where T : UIElement => RootElement.FindElementWithId<T>(id);
        public T? Raycast<T>(Vector2 position) where T : UIElement => RootElement.Raycast<T>(position);

        public void Focus()
        {
            _owningWindow.TakeFocus();
        }

        public void Blur()
        {
            
        }

        public T OpenPopup<T>(LayoutSnippet snippet, Int2? origin = null) where T : SnippetPopupHost
        {
            Int2 startPosition = origin ?? _interactionManager.MousePosition.AsInt2();
            startPosition += _owningWindow.Position;

            PopupManager popup = UIManager.Instance.PopupManager;
            return popup.OpenPopup<T>(snippet, this, startPosition);
        }

        public virtual void RefreshTreeStyling()
        {
            Recursive(RootElement);

            static void Recursive(UIElement element)
            {
                element.InvalidateAll();

                foreach (UIElement child in element.Children)
                {
                    Recursive(child);
                }
            }
        }

        public bool AddStylesheet(StylesheetAsset asset)
        {
            if (_styleProvider.AddStylesheet(asset))
            {
                _isStyleFullyInvalid = true;
                return true;
            }

            return false;
        }

        public bool RemoveStylesheet(StylesheetAsset asset)
        {
            if (_styleProvider.RemoveStylesheet(asset))
            {
                _isStyleFullyInvalid = true;
                return true;
            }

            return false;
        }

        [StackTraceHidden] void IWindow.OnLayoutRecalculatedCallback() { RecalculateLayout(); LayoutRecalculated?.Invoke(); }
        [StackTraceHidden] void IWindow.OnFocusGainedCallback() => OnFocusGained?.Invoke();
        [StackTraceHidden] void IWindow.OnFocusLostCallback() => OnFocusLost?.Invoke();
        [StackTraceHidden] void IWindow.OnShownCallbacks() => OnShown?.Invoke();
        [StackTraceHidden] void IWindow.OnHiddenCallback() => OnHidden?.Invoke();

        public abstract void InvalidateVisualRegion(Boundaries boundaries);

        //////////////// IWindowHost ////////////////

        public virtual void RecalculateLayout()
        {
            Int2 currentSize = Int2.Max(GetContentSize(), Int2.One);
            if (currentSize != _clientSize)
            {
                Display display = Engine.GlobalSingleton.WindowManager.GetDisplayForPoint(_originPosition) ?? _parentWindow.Display;

                Rect usableRect = display.UsableBoundaries;
                Int2 clientPosition = Int2.Clamp(_originPosition, usableRect.Position, usableRect.Position + usableRect.Size);

                if (clientPosition != _owningWindow.Position)
                    _owningWindow.Position = clientPosition;

                _owningWindow.ClientSize = currentSize;
                _clientSize = currentSize;

                _rhiTexture?.Dispose();
                _rhiTexture = null;
            }
        }

        public virtual void DrawVisual(UIPainterContext painter)
        {

        }

        public abstract void TryChangeWindowSize(IWindow window, Int2 newClientSize);

        public virtual void AddStateFlags(UIStateFlags flags)
        {
            _stateFlags |= flags;
        }

        public virtual void RemoveStateFlags(UIStateFlags flags)
        {
            _stateFlags &= ~flags;
        }

        private RHITexture? CreateRHITexture()
        {
            RecalculateLayout();

            if (_clientSize.X <= 0 || _clientSize.Y <= 0 || _disposedValue)
            {
                _rhiTexture?.Dispose();
                _rhiTexture = null;

                return null;
            }

            bool createNew = _rhiTexture == null || (new Int2(_rhiTexture.Description.Width, _rhiTexture.Description.Height) != _clientSize);
            if (createNew)
            {
                _rhiTexture?.Dispose();
                _rhiTexture = null;

                _rhiTexture = RHIDevice.Instance?.CreateTexture(new RHITextureDescription
                {
                    Width = _clientSize.X,
                    Height = _clientSize.Y,

                    MipLevels = 1,

                    Usage = RHIResourceUsage.RenderTarget | RHIResourceUsage.ShaderResource,
                    Dimension = RHIDimension.Texture2D,
                    Format = RHIFormat.RGB10A2_UNorm
                }, [], "PopupHost");
            }

            return _rhiTexture;
        }

        #region IWindow
        public abstract UIElement RootElement { get; }

        public Int2 ClientSize => _clientSize;

        public StyleProvider StyleProvider => _styleProvider;
        public StyleUpdater StyleUpdater => _styleUpdater;

        public IWindowHost? ParentHost => this;
        public abstract Boundaries InvalidVisualRegion { get; }

        public event Action? LayoutRecalculated;

        public event Action? OnFocusGained;
        public event Action? OnFocusLost;

        public event Action? OnShown;
        public event Action? OnHidden;
        #endregion
        #region IInteractable
        public virtual IInteractable GetInteractable(Vector2 point)
        {
            return RootElement;
        }

        public virtual void HandleEvent(ref readonly UIEvent @event)
        {
        }

        public IWindowHost? Host => null;
        public virtual IInteractionShape? Shape => null;
        #endregion
        #region IWindowHost
        public Rect HostMetrics => new Rect(Int2.Zero, _clientSize);
        public Rect ContentMetrics => new Rect(Int2.Zero, _clientSize);

        public HostInteractionManager InteractionManager => _interactionManager;

        public IWindow? ActiveWindow => this;

        public RHITexture? HostTexture => _rhiTexture ?? CreateRHITexture();
        public Window? HostWindow => _owningWindow;

        public UIStateFlags InvalidationFlags => _stateFlags;
        #endregion
    }
}
