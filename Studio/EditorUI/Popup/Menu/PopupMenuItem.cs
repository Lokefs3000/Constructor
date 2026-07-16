using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Input;
using EditorUI.Styling;
using EditorUI.Visual;
using EditorUI.Widgets;

namespace EditorUI.Popup.Menu
{
    public abstract class PopupMenuItem : StyledObject, IInteractable
    {
        protected StateFlags _stateFlags;
        protected float _itemWidth;
        protected float _itemHeight;

        protected Vector2 _position;

        public PopupMenuItem()
        {
            _stateFlags = StateFlags.SelfInvalidStyle;
        }

        protected internal virtual void DestroySelf()
        {
            ClearSavedData();
            ForgetState();
        }

        protected internal abstract void MeasureSelf(float maxAvailableWidth);
        protected internal abstract void PaintSelf(ref readonly PainterContext painter, Vector2 originPosition, Vector4 safePadding, float availableWidth);
        public abstract bool HandleEventSelf(ref readonly UIInputEvent inputEvent);

        protected internal abstract void ClearSavedData();
        protected internal virtual void ForgetState()
        {
            UIManager.Instance.InputManager.ForgetInteractable(this);
        }

        public virtual IInteractable GetInteractable(Vector2 point) => this;

        public virtual IInteractionShape? Shape => null;
        public virtual WidgetInputState InputState => WidgetInputState.Sink;

        public override StateFlags StateFlags => _stateFlags;

        public float ItemWidth => _itemWidth;
        public float ItemHeight => _itemHeight;

        public Vector2 Position { get => _position; internal set => _position = value; }

        public abstract PopupMenu OwningMenu { get; }
        public abstract PopupMenuMenu? OwningItem { get; }
    }
}
