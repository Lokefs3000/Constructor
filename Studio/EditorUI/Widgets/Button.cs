using System;
using System.Collections.Generic;
using System.Text;
using EditorUI.Input;

namespace EditorUI.Widgets
{
    [UIWidget]
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
                        _isHovered = true;
                        SetEditedField(true, nameof(IsHovered));
                        return true;
                    }
                case UIInputEventType.MouseLeave:
                    {
                        _isHovered = false;
                        SetEditedField(false, nameof(IsHovered));
                        return true;
                    }

                case UIInputEventType.MouseDown:
                    {
                        _isHeld = true;
                        SetEditedField(true, nameof(IsHeld));
                        return true;
                    }
                case UIInputEventType.MouseUp:
                    {
                        _isHeld = false;
                        SetEditedField(false, nameof(IsHeld));
                        return true;
                    }
            }

            return false;
        }

        #region Serializable
        [StyleTrigger, Styled(nameof(_isHovered), isEditable: true)]
        public bool IsHovered { get => _isHovered; }

        [StyleTrigger, Styled(nameof(_isHeld), isEditable: true)]
        public bool IsHeld { get => _isHeld; }
        #endregion
    }
}
