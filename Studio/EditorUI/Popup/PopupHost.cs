using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Input;
using EditorUI.Styling;
using EditorUI.Visual;
using EditorUI.Widgets;
using Primary.Windowing;

namespace EditorUI.Popup
{
    public abstract class PopupHost : StyledObject, IInteractable
    {
        protected StateFlags _stateFlags;

        public PopupHost()
        {
            _stateFlags = StateFlags.SelfInvalidStyle;
        }

        protected internal abstract void StartHostingSelf(Window window);

        protected internal virtual void CleanupSelf()
        {
            UIManager.Instance.InputManager.ForgetInteractable(this);
            OnMenuClosing?.Invoke();
        }

        protected internal abstract void UpdateSelf(Window window);
        protected internal abstract void PaintSelf(ref readonly PainterContext painter, Window window);
        public abstract bool HandleEventSelf(ref readonly UIInputEvent inputEvent);

        public virtual IInteractable GetInteractable(Vector2 point) => this;

        protected internal override void GetUnstyledObjects(ref StyleQueueContext context)
        {
        }

        public override void AddStateFlags(StateFlags flags)
        {
            _stateFlags |= flags;
            OnStateFlagsAdded?.Invoke(flags & ~StateFlags.This);
        }

        public override void RemoveStateFlags(StateFlags flags)
        {
            _stateFlags &= ~flags;
        }

        public virtual IInteractionShape? Shape => null;
        public virtual WidgetInputState InputState => WidgetInputState.Sink;

        protected internal override StyledObject? ParentObject => null;

        public override StateFlags StateFlags => _stateFlags;

        public event Action? OnMenuClosing;
        public event Action<StateFlags>? OnStateFlagsAdded;
    }
}
