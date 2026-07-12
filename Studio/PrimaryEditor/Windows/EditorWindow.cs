using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CommunityToolkit.Diagnostics;
using EditorUI;
using EditorUI.Serialization;
using EditorUI.Styling;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Threading;
using PrimaryEditor.Assets;

namespace PrimaryEditor.Windows
{
    public abstract class EditorWindow : WidgetWindow
    {
        private UILayoutAsset? _layoutAsset;
        private int _currentLayoutLoadIndex;

        protected EditorWindow(WindowManager windowManager, ValueSerializer valueSerializer) : base(windowManager, valueSerializer)
        {
            _layoutAsset = null;
        }

        protected override void DestroySelf()
        {
            if (_layoutAsset != null)
            {
                AssetManager.ListenForAssetLoad(_layoutAsset, this, null);
            }

            base.DestroySelf();
        }

        protected void LoadLayout(string assetName)
        {
            if (_layoutAsset != null)
                throw new InvalidOperationException("Cannot load layout when another one has been previously loaded");

            _layoutAsset = AssetManager.LoadAsset<UILayoutAsset>(assetName);
            AssetManager.ListenForAssetLoad(_layoutAsset, this, OnAssetCallback);
        }

        private void OnAssetCallback(UILayoutAsset layoutAsset, bool wasReloaded)
        {
            Guard.IsNotNull(layoutAsset);

            _currentLayoutLoadIndex = _layoutAsset!.LoadIndex;
            if (_layoutAsset.Status == ResourceStatus.Success)
            {
                CleanupReloadSelf();

                foreach (Widget childWidget in RootWidget.Children)
                {
                    childWidget.Destroy();
                }

                StylesheetProvider.ClearStylesheets();

                UIManager.Instance.ActionScheduler.Schedule((_) =>
                {
                    Widget widget = _layoutAsset.Instantiate()!;

                    foreach (StylesheetAsset stylesheetAsset in _layoutAsset.Stylesheets)
                    {
                        Stylesheet? stylesheet = stylesheetAsset.WaitIfNotLoaded().Stylesheet;
                        if (stylesheet != null)
                            StylesheetProvider.AddStylesheet(stylesheet);
                    }

                    RootWidget.AddChild(widget);

                    InitializeSelf();
                }, null);
            }
        }

        protected internal abstract void InitializeSelf();
        protected internal abstract void CleanupReloadSelf();

        internal UILayoutAsset? LayoutAsset => _layoutAsset;
        internal int CurrentLayoutLoadIndex => _currentLayoutLoadIndex;
    }
}
