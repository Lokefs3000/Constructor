using Editor.UI.Datatypes;
using Editor.UI.Debugging;
using Editor.UI.Elements;
using Editor.UI.Styling;
using Primary.Common;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Editor.UI
{
    public class UIWindow
    {
        private readonly int _uniqueWindowId;
        private readonly UIElement _rootElement;

        private readonly StyleProvider _styleProvider;
        private readonly StyleUpdater _styleUpdater;

        private IWindowHost? _parentHost;
        private Boundaries? _invalidVisualRegion;

        private string _windowTitle;

        private Int2 _clientSize;

        public UIWindow(int uniqueWindowId)
        {
            _uniqueWindowId = uniqueWindowId;
            _rootElement = new UIElement();

            _styleProvider = new StyleProvider();
            _styleUpdater = new StyleUpdater();

            _parentHost = null;
            _invalidVisualRegion = null;

            _windowTitle = GetType().Name;

            _rootElement.Size = UIValue2.Max;
            _rootElement.SetNewAndUpdateChildren(this);
        }

        protected virtual void InitializePostLoad() { }
        internal void CallPostLoadInit() { InitializePostLoad(); }

        public void SetClientSizeFromHost(Int2 clientSize) => _clientSize = clientSize;
        
        internal void InvalidateVisualRegion(Boundaries boundaries)
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

        public T? FindElementWithId<T>(string id) where T : UIElement => _rootElement.FindElementWithId<T>(id);
        public T? Raycast<T>(Vector2 position) where T : UIElement => _rootElement.Raycast<T>(position);

        #region Event invokers
        [StackTraceHidden]
        internal void Invoke_LayoutRecalculated() => LayoutRecalculated?.Invoke();
        #endregion

        public int UniqueWindowId => _uniqueWindowId;
        public UIElement RootElement => _rootElement;

        public StyleProvider StyleProvider => _styleProvider;
        public StyleUpdater StyleUpdater => _styleUpdater;

        public IWindowHost? ParentHost { get => _parentHost; internal set => _parentHost = value;  }
        public Boundaries InvalidVisualRegion => _invalidVisualRegion.GetValueOrDefault(Boundaries.Zero);

        public string WindowTitle { get => _windowTitle; set => _windowTitle = value; }

        public Int2 ClientSize { get => _clientSize; set => _parentHost?.TryChangeWindowSize(this, value); }

        #region Events
        public event Action? LayoutRecalculated;
        #endregion
    }
}
