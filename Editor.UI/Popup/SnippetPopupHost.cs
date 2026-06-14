using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Editor.UI.Serialization;
using Editor.UI.Visual;
using Primary.Common;
using Primary.Mathematics;
using Primary.Windowing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Editor.UI.Popup
{
    public class SnippetPopupHost : PopupHost
    {
        private UIWindowRoot _rootElement;
        private LayoutSnippet? _snippet;

        private Boundaries _invalidBoundaries;

        public SnippetPopupHost(Window parentWindow, Window owningWindow, Int2 originPosition) : base(parentWindow, owningWindow, originPosition)
        {
            _rootElement = new UIWindowRoot(this) { Size = new UIValue2(32, 32) };
            _snippet = null;

            _invalidBoundaries = Boundaries.Zero;

            UIFitterLayout fitterLayout = _rootElement.AddLayoutModifier<UIFitterLayout>();
            fitterLayout.Margin = new UIValue2(1, 1);
            fitterLayout.Axis = UIFitterAxis.Both;
        }

        private void ChangeActiveSnippet(LayoutSnippet? snippet)
        {
            if (_snippet != null)
            {
                _snippet.RootElement?.ClearParent();
                _snippet.OnCleanup -= OnSnippetCleanup;
                _snippet.OnReloaded -= OnSnippetReloaded;

                StylesheetAsset[] currentStylesheets = StyleProvider.Stylesheets.ToArray();
                foreach (StylesheetAsset stylesheet in currentStylesheets)
                {
                    RemoveStylesheet(stylesheet);
                }
            }

            if (snippet != null)
            {
                snippet.RootElement?.Parent = _rootElement;
                snippet.OnCleanup += OnSnippetCleanup;
                snippet.OnReloaded += OnSnippetReloaded;

                foreach (StylesheetAsset stylesheet in snippet.Stylesheets)
                {
                    AddStylesheet(stylesheet);
                }
            }
        }

        private void OnSnippetCleanup(LayoutSnippet snippet, bool isBeginDestroyed)
        {
            if (snippet.RootElement?.Parent == _rootElement)
            {
                foreach (StylesheetAsset stylesheet in snippet.Stylesheets)
                {
                    RemoveStylesheet(stylesheet);
                }

                if (isBeginDestroyed)
                    snippet.RootElement?.ClearParent();
                else
                    _snippet = null;
            }
        }

        private void OnSnippetReloaded(LayoutSnippet snippet)
        {
            foreach (StylesheetAsset stylesheet in snippet.Stylesheets)
            {
                AddStylesheet(stylesheet);
            }

            snippet.RootElement?.Parent = _rootElement;
        }

        protected override void CleanupSelf()
        {
            _rootElement.Destroy();
        }

        protected override Int2 GetContentSize()
        {
            return _rootElement.CurrentSize.AsInt2();
        }

        public override void InvalidateVisualRegion(Boundaries boundaries)
        {
            _invalidBoundaries = Boundaries.Union(_invalidBoundaries, boundaries);
        }

        public override void TryChangeWindowSize(IWindow window, Int2 newClientSize)
        {
            Debug.Assert(window == this);
            _rootElement.Size = new UIValue2(newClientSize.X, newClientSize.Y);
        }

        public LayoutSnippet? Snippet { get => _snippet; set => ChangeActiveSnippet(value); }

        public override UIElement RootElement => _rootElement;
        public override Boundaries InvalidVisualRegion => _invalidBoundaries;
    }
}
