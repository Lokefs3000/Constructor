using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI
{
    public class UIWindow
    {
        private readonly int _uniqueWindowId;
        private readonly UIElement _rootElement;

        private UIDockHost? _parentHost;

        private string _windowTitle;

        private Vector2 _clientSize;

        public UIWindow(int uniqueWindowId)
        {
            _uniqueWindowId = uniqueWindowId;
            _rootElement = new UIElement();

            _parentHost = null;

            _windowTitle = "UIWindow";

            _rootElement.Transform.Size = UIValue2.Max;
            _rootElement.SetNewAndUpdateChildren(this, 0);
        }

        internal void RemoveInvalidFlags(UIInvalidationFlags flags) => _rootElement.RemoveInvalidFlag(flags);
        internal void SetClientSizeFromHost(Vector2 clientSize) => _clientSize = clientSize;

        public int UniqueWindowId => _uniqueWindowId;
        public UIElement RootElement => _rootElement;

        public UIInvalidationFlags InvalidFlags => _rootElement.InvalidFlags;
        public Boundaries InvalidVisualRegion => _rootElement.InvalidVisualRegion;

        public UIDockHost? ParentHost { get => _parentHost; internal set => _parentHost = value;  }

        public string WindowTitle { get => _windowTitle; set => _windowTitle = value; }

        public Vector2 ClientSize { get => _clientSize; set => _parentHost?.TryChangeWindowSize(this, value); }
    }
}
