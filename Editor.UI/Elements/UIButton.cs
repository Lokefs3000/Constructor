using Editor.UI.Datatypes;
using Editor.UI.Interaction;
using Editor.UI.Visual;
using Primary.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Editor.UI.Elements
{
    [UIElementPrettyName("Button")]
    [UIElementStates("Normal", "Hovered", "Pressed")]
    public class UIButton : UIFrame
    {
        public UIButton()
        {

        }

        public override void HandleEvent(HostInteractionManager interaction, ref readonly UIEvent @event)
        {
            switch (@event.Type)
            {
                case UIEventType.MouseEnter: SetState("Hovered", true); break;
                case UIEventType.MouseLeave: SetState("Hovered", false); break;
                case UIEventType.MouseActivate: SetState("Pressed", true); break;
                case UIEventType.MouseDeactivate: SetState("Pressed", false); break;
            }
        }
    }
}
