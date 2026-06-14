using Editor.UI.Assets;
using Editor.UI.Elements;
using Primary.Collections.ReadOnly;
using Primary.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Editor.UI.Serialization
{
    public class LayoutSnippet
    {
        private readonly string? _snippetFile;

        private List<StylesheetAsset> _stylesheets;
        private UIElement? _rootElement;

        public LayoutSnippet(string? snippetFile)
        {
            _snippetFile = snippetFile;

            _stylesheets = new List<StylesheetAsset>();
            _rootElement = null;
        }

        protected virtual void SetupSelf() { }
        protected virtual void CleanupSelf() { }

        internal void ClearData()
        {
            CleanupSelf();

            _rootElement?.Destroy();

            _stylesheets.Clear();
            _rootElement = null;
        }

        internal bool AddStylesheet(StylesheetAsset asset) => _stylesheets.AddUnique(asset);

        #region Invokers
        [StackTraceHidden] internal void Invoke_SetupSelf() => SetupSelf();

        [StackTraceHidden] internal void Invoke_OnCleanup(bool isBeginDestroyed) => OnCleanup?.Invoke(this, isBeginDestroyed);
        [StackTraceHidden] internal void Invoke_OnReloaded() => OnReloaded?.Invoke(this);
        #endregion

        public string? SnippetFile => _snippetFile;

        public ROList<StylesheetAsset> Stylesheets => _stylesheets;
        public UIElement? RootElement { get => _rootElement; internal set => _rootElement = value; }

        #region Events
        public event Action<LayoutSnippet, bool>? OnCleanup;
        public event Action<LayoutSnippet>? OnReloaded;
        #endregion
    }
}
