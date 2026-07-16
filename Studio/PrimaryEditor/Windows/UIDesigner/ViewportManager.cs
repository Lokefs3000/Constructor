using System;
using System.Collections.Generic;
using System.Text;
using EditorUI;
using EditorUI.Popup.Menu;
using EditorUI.Reflection.Cache;
using EditorUI.Widgets;
using Primary.Assets;
using Primary.Input.Devices;
using PrimaryEditor.Assets;
using PrimaryEditor.Core;

namespace PrimaryEditor.Windows.UIDesigner
{
    internal sealed class ViewportManager
    {
        private readonly UIDesignerWindow _window;

        private readonly ContextMenu _viewportContextMenu;

        internal ViewportManager(UIDesignerWindow window)
        {
            _window = window;

            TextureAtlasAsset genericAtlas = AssetManager.LoadAsset<TextureAtlasAsset>("Editor/UI/Icons/ContextMenu/ContextMenuIcons.atlas").WaitIfNotLoaded();
            TextureAtlasAsset widgetTypesAtlas = AssetManager.LoadAsset<TextureAtlasAsset>("Editor/UI/Icons/ContextMenu/UIDesignerWidgets.atlas").WaitIfNotLoaded();

            _viewportContextMenu = new ContextMenu();
            PopupMenuMenu addMenu = _viewportContextMenu.AddMenu("add", "Add");
            _viewportContextMenu.AddSeparator();
            _viewportContextMenu.AddAction("cut", "Cut", genericAtlas.TryFindSpriteOrNull("Cut"));
            _viewportContextMenu.AddAction("copy", "Copy", genericAtlas.TryFindSpriteOrNull("Copy"));
            _viewportContextMenu.AddAction("delete", "Delete", genericAtlas.TryFindSpriteOrNull("Delete"));
            _viewportContextMenu.AddAction("remame", "Rename", genericAtlas.TryFindSpriteOrNull("Rename"));

            UIManager ui = UIManager.Instance;
            foreach (var (name, type) in ui.ReflectionManager.WidgetDatabase.Entries)
            {
                if (type.IsAssignableTo(typeof(Widget)))
                {
                    WidgetCachedData cachedData = ui.ReflectionManager.WidgetPropertyCache.GetCachedData(type);
                    if (cachedData.Constructor != null)
                        addMenu.AddAction($"add_{name}", name, widgetTypesAtlas.TryFindSpriteOrNull(name) ?? widgetTypesAtlas.TryFindSpriteOrNull("NoImg"));
                }
            }

            _viewportContextMenu.OnItemPressed += OnViewportCtxItemPressed;
        }

        internal void Destroy()
        {
            _viewportContextMenu.Destroy();
        }

        internal void Initialize()
        {
            UIManager ui = EditorRuntime.Instance.UIManager;

            Widget centerPanel = _window.RootWidget.FindWidgetWithId<Widget>("panel-center", true)!;
            centerPanel.OnMousePress += (_, button) => { if (button.Button == MouseButton.Right) { UIManager.Instance.PopupManager.OpenMenu(_viewportContextMenu); } };
        }

        internal void Cleanup()
        {

        }

        private void OnViewportCtxItemPressed(string actionName)
        {

        }
    }
}
