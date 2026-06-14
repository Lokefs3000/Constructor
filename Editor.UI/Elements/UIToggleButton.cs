using Editor.UI.Interaction;
using Primary.Input.Devices;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("ToggleButton"), StyleableStates("Normal", "Hovered", "Pressed", "Toggled")]
    public class UIToggleButton : UIButton
    {
        private bool _isToggled;

        public UIToggleButton()
        {
            _isToggled = false;
        }

        public UIToggleButton(UIElement parent) : this()
        {
            SetParent(parent);
        }

        public override void HandleEvent(ref readonly UIEvent @event)
        {
            if (@event.Type == UIEventType.MouseActivate && @event.Mouse.Button == MouseButton.Left)
            {
                _isToggled = !_isToggled;
                OnToggle?.Invoke(_isToggled);

                SetState("Toggled", _isToggled);
            }

            base.HandleEvent(in @event);
        }

        #region Properties
        [EditableProperty(nameof(_isToggled), UIStateFlags.InvalidVisual)] public bool IsToggled { get => _isToggled; set { SetEditableProperty(value); SetState("Toggled", value); } }
        #endregion
        #region Events
        public event Action<bool>? OnToggle;
        #endregion
    }
}
