using Editor.UI.Assets;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Profiling;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Styling
{
    public sealed class StyleManager
    {
        private readonly UIManager _manager;

        private ClassListCache _classListCache;

        internal StyleManager(UIManager uiManager)
        {
            _manager = uiManager;

            _classListCache = new ClassListCache(uiManager);

            AssetManager.AddAssetReloadedCallback<StylesheetAsset>(StylesheetReloaded);
        }

        internal void RefreshStyles()
        {
            using (new ProfilingScope("RefreshStyles"))
            {
                foreach (var kvp in UIManager.Instance.WindowManager.Active)
                {
                    if (kvp.Value.StyleUpdater.HasInvalidStyleBases)
                    {
                        kvp.Value.StyleUpdater.UpdateAll(kvp.Value.WindowTitle.Length == 0 ? null : kvp.Value.WindowTitle);
                    }
                }

                foreach (var kvp in UIManager.Instance.PopupManager.Popups)
                {
                    if (kvp.Host.StyleUpdater.HasInvalidStyleBases)
                    {
                        kvp.Host.StyleUpdater.UpdateAll(kvp.Host.ToString());
                    }
                }
            }
        }

        private void StylesheetReloaded(Type type, AssetId id)
        {
            StylesheetAsset asset = AssetManager.LoadAsset<StylesheetAsset>(id);
            foreach (var kvp in _manager.WindowManager.Active)
            {
                UIWindow window = kvp.Value;
                if (window.StyleProvider.ContainsStylesheet(asset))
                {
                    window.StyleProvider.ReloadStylesheet(asset);
                    window.RefreshTreeStyling();
                }
            }

            foreach (var kvp in UIManager.Instance.PopupManager.Popups)
            {
                IWindow window = kvp.Host;
                if (window.StyleProvider.ContainsStylesheet(asset))
                {
                    window.StyleProvider.ReloadStylesheet(asset);
                    window.RefreshTreeStyling();
                }
            }
        }

        public ClassListCache ClassListCache => _classListCache;
    }
}
