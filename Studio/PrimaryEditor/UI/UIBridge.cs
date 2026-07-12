using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Styling;
using EditorUI.Windowing;
using Primary.Assets;
using Primary.Assets.Types;
using PrimaryEditor.Assets;
using PrimaryEditor.Core;
using PrimaryEditor.Windows;

namespace PrimaryEditor.UI
{
    internal sealed class UIBridge
    {
        private EditorRuntime _editor;

        internal UIBridge(EditorRuntime editor)
        {
            _editor = editor;

            // AssetManager.AddAssetReloadedCallback<UILayoutAsset>(LayoutAssetLoad);
            AssetManager.AddAssetReloadedCallback<StylesheetAsset>(StylesheetAssetLoad);
        }

        private void LayoutAssetLoad(Type type, AssetId id)
        {
            foreach (WindowBase window in _editor.UIManager.WindowManager.Windows)
            {
                if (window is EditorWindow editorWindow)
                {
                    if (editorWindow.LayoutAsset != null && editorWindow.LayoutAsset.Id == id && editorWindow.LayoutAsset.LoadIndex > editorWindow.CurrentLayoutLoadIndex)
                    {

                    }
                }
            }
        }

        private void StylesheetAssetLoad(Type type, AssetId id)
        {
            StylesheetAsset asset = AssetManager.LoadAsset<StylesheetAsset>(id);
            if (asset.Status == ResourceStatus.Success)
            {
                Stylesheet newStylesheet = asset.Stylesheet!;
                _editor.UIManager.StyleManager.ReplaceStylesheetFromName(newStylesheet.SourceName, newStylesheet);
            }
        }
    }
}
