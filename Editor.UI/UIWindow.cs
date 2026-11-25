using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Diagnostics;
using Editor.UI.Elements;
using Editor.UI.Popup;
using Editor.UI.Serialization;
using Editor.UI.Styling;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Editor.UI
{
    public class UIWindow : IWindow, IRenderableWindow, IElementOwner
    {
        private readonly int _uniqueWindowId;
        private readonly UIElement _rootElement;

        private readonly StyleProvider _styleProvider;
        private readonly StyleUpdater _styleUpdater;

        private bool _isStyleFullyInvalid;

        private IWindowHost? _parentHost;
        private Boundaries? _invalidVisualRegion;

        private string _windowTitle;

        private Int2 _clientSize;

        private string? _layoutFile;

        public UIWindow(int uniqueWindowId)
        {
            _uniqueWindowId = uniqueWindowId;
            _rootElement = new UIElement();

            _styleProvider = new StyleProvider();
            _styleUpdater = new StyleUpdater();

            _isStyleFullyInvalid = false;

            _parentHost = null;
            _invalidVisualRegion = null;

            _windowTitle = GetType().Name;

            _rootElement.Position = new UIValue2(0, 28);
            _rootElement.Size = new UIValue2(0, 1.0f, -28, 1.0f);
            _rootElement.SetNewAndUpdateChildren(this);

            _layoutFile = null;
        }

        protected virtual void InitializePostLoad() { }
        protected virtual void CleanupSelf() { }

        internal void PostLoadInit(string? layoutFile)
        {
            _layoutFile = layoutFile;
            InitializePostLoad();
        }

        internal void Cleanup()
        {
            CleanupSelf();
        }

        public virtual void Update()
        {
            if (_isStyleFullyInvalid)
            {
                RefreshTreeStyling();
                _isStyleFullyInvalid = false;
            }
        }

        public void SetClientSizeFromHost(Int2 clientSize) => _clientSize = clientSize;

        void IWindow.InvalidateVisualRegion(Boundaries boundaries)
        {
            if (_invalidVisualRegion.HasValue)
                _invalidVisualRegion = Boundaries.Union(_invalidVisualRegion.Value, boundaries);
            else
                _invalidVisualRegion = boundaries;
        }

        public void InvalidateTree(UIStateFlags flags)
        {
            _parentHost?.AddStateFlags(flags);
            RecursiveInvalidate(_rootElement);

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
            if (_parentHost is IWindowDockHost dockHost)
                dockHost.FocusWindow(this);
        }

        public void Blur()
        {

        }

        public T OpenPopup<T>(LayoutSnippet snippet, Int2? origin = null) where T : SnippetPopupHost
        {
            Int2 startPosition = origin.GetValueOrDefault(InputSystem.Pointer.GlobalMousePosition);
            if (origin is not null && _parentHost is UIDockHost dockHost)
                startPosition = dockHost.GetDisplayPosition(origin.Value);

            PopupManager popup = UIManager.Instance.PopupManager;
            return popup.OpenPopup<T>(snippet, this, startPosition);
        }

        public void RefreshTreeStyling()
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
            if (StyleProvider.AddStylesheet(asset))
            {
                _isStyleFullyInvalid = true;
                return true;
            }

            return false;
        }

        public bool RemoveStylesheet(StylesheetAsset asset)
        {
            if (StyleProvider.RemoveStylesheet(asset))
            {
                _isStyleFullyInvalid = true;
                return true;
            }

            return false;
        }

        void IWindow.SetWindowHost(IWindowHost? host)
        {
            _parentHost = host;
        }

        public override string ToString()
        {
            return WindowTitle;
        }

        #region Event invokers
        [StackTraceHidden] void IWindow.OnLayoutRecalculatedCallback() => LayoutRecalculated?.Invoke();
        [StackTraceHidden] void IWindow.OnFocusGainedCallback() => OnFocusGained?.Invoke();
        [StackTraceHidden] void IWindow.OnFocusLostCallback() => OnFocusLost?.Invoke();
        [StackTraceHidden] void IWindow.OnShownCallbacks() => OnShown?.Invoke();
        [StackTraceHidden] void IWindow.OnHiddenCallback() => OnHidden?.Invoke();
        #endregion

        public int UniqueWindowId => _uniqueWindowId;
        public UIElement RootElement => _rootElement;

        public StyleProvider StyleProvider => _styleProvider;
        public StyleUpdater StyleUpdater => _styleUpdater;

        public IWindowHost? ParentHost => _parentHost;
        public Boundaries InvalidVisualRegion => _invalidVisualRegion.GetValueOrDefault(Boundaries.Zero);

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    OnWindowTitleChanged?.Invoke(value);
                }
            }
        }

        public Int2 ClientSize { get => _clientSize; set => _parentHost?.TryChangeWindowSize(this, value); }

        public string? LayoutFile => _layoutFile;

        #region Events
        public event Action? LayoutRecalculated;

        public event Action? OnFocusGained;
        public event Action? OnFocusLost;

        public event Action? OnShown;
        public event Action? OnHidden;

        public event Action<string>? OnWindowTitleChanged;
        #endregion
    }
}
