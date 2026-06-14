using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.UI.Interaction
{
    public sealed class HostInteractionManager
    {
        private Vector2 _mousePosition;

        public HostInteractionManager()
        {

        }

        public void DereferenceDestroyedInteractable(IInteractable interactable)
        {
            UIInteractionManager interactionManager = UIManager.Instance.InteractionManager;
            interactionManager.RemoveReferencesFor(interactable);
        }

        internal void HandleFiredEvent(IInteractable interactable, ref UIEvent eventData)
        {
            switch (eventData.Type)
            {
                case UIEventType.MouseMotion: _mousePosition = eventData.Mouse.Position; break;
            }

            EventFired?.Invoke(interactable, new Ref<UIEvent>(ref eventData));
        }

        public Vector2 MousePosition => _mousePosition;

        public event Action<IInteractable, Ref<UIEvent>>? EventFired;
    }
}
