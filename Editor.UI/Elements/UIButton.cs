using Editor.UI.Datatypes;
using Editor.UI.Interaction;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("Button"), StyleableStates("Normal", "Hovered", "Pressed")]
    public class UIButton : UIFrame
    {
        public UIButton()
        {

        }

        public UIButton(UIElement parent) : base()
        {
            SetParent(parent);
        }

        public override void HandleEvent(ref readonly UIEvent @event)
        {
            switch (@event.Type)
            {
                case UIEventType.MouseEnter: SetState("Hovered", true); break;
                case UIEventType.MouseLeave: SetState("Hovered", false); break;
                case UIEventType.MouseDown: SetState("Pressed", true); break;
                case UIEventType.MouseUp: SetState("Pressed", false); break;
                case UIEventType.MouseActivate: OnPressed?.Invoke(); break;
            }
        }

        #region Events
        public event Action? OnPressed;
        #endregion
    }
}
