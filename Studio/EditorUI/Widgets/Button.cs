using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Input;

namespace EditorUI.Widgets
{
    [UIWidget, TriggerValues("is-hovered", "is-held")]
    public class Button : Widget
    {
        protected bool _isHovered;
        protected bool _isHeld;

        public Button()
        {
            _isHovered = false;
            _isHeld = false;
        }

        public override bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            base.HandleEventSelf(in inputEvent);

            switch (inputEvent.EventType)
            {
                case UIInputEventType.MouseEnter:
                    {
                        SetTriggerValue("is-hovered", true);
                        return true;
                    }
                case UIInputEventType.MouseLeave:
                    {
                        SetTriggerValue("is-hovered", false);
                        return true;
                    }

                case UIInputEventType.MouseDown:
                    {
                        SetTriggerValue("is-held", true);
                        return true;
                    }
                case UIInputEventType.MouseUp:
                    {
                        SetTriggerValue("is-held", false);
                        return true;
                    }
            }

            return false;
        }

        #region Serializable
        public bool IsHovered { get => _isHovered; }
        public bool IsHeld { get => _isHeld; }
        #endregion
    }
}
