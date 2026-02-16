using CommunityToolkit.HighPerformance;
using Editor.UI.Elements;
using Primary.Input.Devices;
using SDL;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TerraFX.Interop.Windows;

namespace Editor.UI.Interaction
{
    public sealed class HostInteractionManager
    {
        private readonly UIDockHost _dockHost;

        private UIElement? _currentMouseHover;

        internal HostInteractionManager(UIDockHost dockHost)
        {
            _dockHost = dockHost;

            _currentMouseHover = null;
        }

        internal void HandleMouseMotion(Vector2 mousePosition, Vector2 mouseDelta)
        {
            mousePosition -= _dockHost.ClientOffset;

            UIElement? element = FindTopNodeAtPosition(mousePosition);
            if (element != null)
            {
                TryChangeMouseHover(element, mousePosition);
                FireEventForElement(element, UIEventType.MouseMotion, new UIMouseEvent { Position = mousePosition });
            }
            else
            {
                TryChangeMouseHover(null, mousePosition);
            }
        }

        internal void HandleMouseWheel(Vector2 mousePosition, Vector2 wheelDelta)
        {
            mousePosition -= _dockHost.ClientOffset;
            
            UIElement? element = FindTopNodeAtPosition(mousePosition);
            if (element != null)
            {
                FireEventForElement(element, UIEventType.MouseWheel, new UIMouseEvent { Position = mousePosition, Delta = wheelDelta });
            }
        }

        internal void HandleMouseDown(Vector2 mousePosition, MouseButton button)
        {
            mousePosition -= _dockHost.ClientOffset;

            UIElement? element = FindTopNodeAtPosition(mousePosition);
            if (element != null)
            {
                FireEventForElement(element, UIEventType.MouseButtonDown, new UIMouseEvent { Position = mousePosition, Button = button });
            }
        }

        internal void HandleMouseUp(Vector2 mousePosition, MouseButton button)
        {
            mousePosition -= _dockHost.ClientOffset;

            UIElement? element = FindTopNodeAtPosition(mousePosition);
            if (element != null)
            {
                FireEventForElement(element, UIEventType.MouseButtonUp, new UIMouseEvent { Position = mousePosition, Button = button });
            }
        }

        private void FireEventForElement(UIElement element, UIEvent eventData)
        {
            EventFired?.Invoke(element, new Ref<UIEvent>(ref eventData));
            element.HandleEvent(this, ref eventData);
        }

        private void FireEventForElement(UIElement element, UIEventType eventType, UIMouseEvent mouseEvent) => FireEventForElement(element, new UIEvent(eventType, mouseEvent));

        private void TryChangeMouseHover(UIElement? newTarget, Vector2 mousePosition)
        {
            if (_currentMouseHover != null && _currentMouseHover != newTarget)
            {
                FireEventForElement(_currentMouseHover, UIEventType.MouseLeave, new UIMouseEvent { Position = mousePosition });
                _currentMouseHover = null;
            }

            if (_currentMouseHover == null && newTarget != null)
            {
                FireEventForElement(newTarget, UIEventType.MouseEnter, new UIMouseEvent { Position = mousePosition });
            }

            _currentMouseHover = newTarget;
        }

        private UIElement? FindTopNodeAtPosition(Vector2 position)
        {
            return SearchRecursive(position, _dockHost.ActiveWindow!.RootElement);

            static UIElement? SearchRecursive(Vector2 position, UIElement element)
            {
                for (int i = element.Children.Count - 1; i >= 0; --i)
                {
                    UIElement child = element.Children[i];
                    if (child.ElementTreeBounds.IsWithin(position))
                    {
                        UIElement? ret = SearchRecursive(position, child);
                        if (ret != null)
                            return ret;
                    }
                }

                return element.Transform.RenderCoordinates.IsWithin(position) ? element : null;
            }
        }

        internal event Action<UIElement, Ref<UIEvent>>? EventFired;
    }
}
