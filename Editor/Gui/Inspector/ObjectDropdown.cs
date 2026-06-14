using System;
using System.Collections.Generic;
using System.Text;
using Editor.UI.Datatypes;
using Editor.UI.Elements;
using Editor.UI.Modifiers;
using Editor.UI.Text;
using TerraFX.Interop.Windows;

namespace Editor.Gui.Inspector
{
    internal sealed class ObjectDropdown
    {
        private UIElement _rootElement;
        private UIButton _arrowButton;
        private UILabel _label;

        private UIElement _contentElement;

        private bool _hasLoadedChildren;

        public ObjectDropdown()
        {
            _rootElement = new UIElement();
            _arrowButton = new UIButton(_rootElement);
            _label = new UILabel(_rootElement);
            {
                _rootElement.Size = UIValue2.Max;

                UIFitterLayout fitterLayout = _rootElement.AddLayoutModifier<UIFitterLayout>();
                fitterLayout.Axis = UIFitterAxis.Vertical;
                fitterLayout.Margin = new UIValue2(4, 4);

                _arrowButton.Position = new UIValue2(4, 4);
                _arrowButton.Size = new UIValue2((int)TextManager.PixelsPerEM, (int)TextManager.PixelsPerEM);

                _label.Position = new UIValue2((int)TextManager.PixelsPerEM + 8, 4);
                _label.AutoSize = UITextAutoSize.FitBoundsToText;
            }

            _contentElement = new UIElement(_rootElement);
            {
                _contentElement.Position = new UIValue2(16, (int)TextManager.PixelsPerEM + 4);
                _contentElement.Size = new UIValue2(-16, 1.0f, 0, 1.0f);

                _contentElement.IsEnabled = false;

                UIListLayout listLayout = _contentElement.AddLayoutModifier<UIListLayout>();
                listLayout.Direction = UIListLayoutDirection.Vertical;
                listLayout.Padding = new UIValue(2);

                UIFitterLayout fitterLayout = _contentElement.AddLayoutModifier<UIFitterLayout>();
                fitterLayout.Axis = UIFitterAxis.Vertical;
                fitterLayout.Margin = new UIValue2(4, 4);
            }

            _hasLoadedChildren = false;

            _arrowButton.OnPressed += ExpandButtonPressed;
        }

        public void Destroy()
        {
            _arrowButton.OnPressed -= ExpandButtonPressed;

            _rootElement.Destroy();
        }

        private void ExpandButtonPressed()
        {
            if (!_hasLoadedChildren && !_contentElement.IsEnabled)
            {
                OnLoadDropdownElements?.Invoke();
                _hasLoadedChildren = true;
            }

            _contentElement.IsEnabled = !_contentElement.IsEnabled;
        }

        public UIElement RootElement => _rootElement;
        public UIButton ArrowButton => _arrowButton;
        public UILabel Label => _label;

        public UIElement ContentElement => _contentElement;

        public event Action? OnLoadDropdownElements;
    }
}
