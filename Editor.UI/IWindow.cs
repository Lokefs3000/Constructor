using Editor.UI.Assets;
using Editor.UI.Elements;
using Editor.UI.Popup;
using Editor.UI.Serialization;
using Editor.UI.Styling;
using Primary.Common;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI
{
    public interface IWindow : IElementOwner
    {
        public UIElement RootElement { get; }

        public Boundaries InvalidVisualRegion { get; }

        public Int2 ClientSize { get; }

        public void Update();

        public void SetWindowHost(IWindowHost? host);

        public void SetClientSizeFromHost(Int2 clientSize);
        public void InvalidateTree(UIStateFlags flags);

        public T? FindElementWithId<T>(string id) where T : UIElement => RootElement.FindElementWithId<T>(id);
        public T? Raycast<T>(Vector2 position) where T : UIElement => RootElement.Raycast<T>(position);

        public void Focus();
        public void Blur();

        public T OpenPopup<T>(LayoutSnippet snippet, Int2? origin = null) where T : SnippetPopupHost;

        public void RefreshTreeStyling();

        public bool AddStylesheet(StylesheetAsset stylesheet);
        public bool RemoveStylesheet(StylesheetAsset stylesheet);

        public void OnLayoutRecalculatedCallback();

        public void OnFocusGainedCallback();
        public void OnFocusLostCallback();

        public void OnShownCallbacks();
        public void OnHiddenCallback();

        public void InvalidateVisualRegion(Boundaries boundaries);

        public event Action? LayoutRecalculated;

        public event Action? OnFocusGained;
        public event Action? OnFocusLost;

        public event Action? OnShown;
        public event Action? OnHidden;
    }
}
