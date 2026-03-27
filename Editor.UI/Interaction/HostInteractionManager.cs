using CommunityToolkit.HighPerformance;
using Editor.UI.Elements;
using Primary.Input;
using Primary.Input.Devices;
using Primary.Mathematics;
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
        private EventDispatcher _dispatcher;

        private UIElement? _currentMouseHover;
        private UIElement?[] _currentMouseActivation;

        private Vector2 _dragStartPosition;
        private bool _isDragCurrentlyActive;

        internal HostInteractionManager(UIDockHost dockHost)
        {
            _dockHost = dockHost;
            _dispatcher = new EventDispatcher();

            _currentMouseHover = null;
            _currentMouseActivation = [null, null, null];

            _dragStartPosition = Vector2.Zero;
            _isDragCurrentlyActive = false;
        }

        internal void DeferenceDestroyedElement(UIElement element)
        {
            if (_currentMouseHover == element)
            {
                _currentMouseHover = null;
                HandleMouseMotion(InputSystem.Pointer.MousePosition, InputSystem.Pointer.MouseDelta);
            }

            if (_isDragCurrentlyActive && _currentMouseActivation[(int)MouseButton.Left] == element)
            {
                _isDragCurrentlyActive = false;
                _dispatcher.EndCurrentDrag(element, InputSystem.Pointer.MousePosition);
            }

            for (int i = 0; i < _currentMouseActivation.Length; i++)
            {
                if (_currentMouseActivation[i] == element)
                {
                    _currentMouseActivation[i] = element;
                    HandleMouseDown(InputSystem.Pointer.MouseDelta, (MouseButton)i);
                }
            }
        }

        internal void HandleMouseMotion(Vector2 mousePosition, Vector2 mouseDelta)
        {
            mousePosition -= _dockHost.ClientOffset.AsVector2();

            UIElement? element = FindTopNodeAtPosition(mousePosition);
            if (element != null)
            {
                TryChangeMouseHover(element, mousePosition);
                FireEventForElement(element, UIEventType.MouseMotion, new UIMouseEvent { Position = mousePosition });

                element?.Invoke_OnMouseMove(mousePosition);
            }
            else
            {
                TryChangeMouseHover(null, mousePosition);
            }

            if (_isDragCurrentlyActive)
            {
                _dispatcher.UpdateCurrentDrag(_currentMouseActivation[(int)MouseButton.Left]!, mousePosition);
            }
            else
            {
                if (_currentMouseActivation[(int)MouseButton.Left] != null && Vector2.Distance(_dragStartPosition, mousePosition) > 8.0f)
                {
                    _isDragCurrentlyActive = true;

                    _dispatcher.SetupForNewDrag(_currentMouseActivation[(int)MouseButton.Left]!, _dragStartPosition, mousePosition);
                }
            }
        }

        internal void HandleMouseWheel(Vector2 mousePosition, Vector2 wheelDelta)
        {
            mousePosition -= _dockHost.ClientOffset.AsVector2();
            
            UIElement? element = FindTopNodeAtPosition(mousePosition);
            if (element != null)
            {
                FireEventForElement(element, UIEventType.MouseWheel, new UIMouseEvent { Position = mousePosition, Delta = wheelDelta });
            }
        }

        internal void HandleMouseDown(Vector2 mousePosition, MouseButton button)
        {
            Vector2 localMousePosition = mousePosition - _dockHost.ClientOffset.AsVector2();

            UIElement? element = FindTopNodeAtPosition(localMousePosition);
            if (element != null)
            {
                TryChangeMouseActivation(element, localMousePosition, button);
                
                if (button == MouseButton.Left)
                {
                    _dragStartPosition = mousePosition;
                }
            }
        }

        internal void HandleMouseUp(Vector2 mousePosition, MouseButton button)
        {
            mousePosition -= _dockHost.ClientOffset.AsVector2();

            UIElement? active = _currentMouseActivation[(int)button];
            if (active != null)
            {
                TryChangeMouseActivation(null, mousePosition, button);

                if (_isDragCurrentlyActive && button == MouseButton.Left && active != null)
                {
                    _isDragCurrentlyActive = false;
                    _dispatcher.EndCurrentDrag(active, _dragStartPosition - mousePosition);
                }
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

        private void TryChangeMouseActivation(UIElement? newTarget, Vector2 mousePosition, MouseButton button)
        {
            if (button == MouseButton.Unknown)
                return;

            ref UIElement? currentTarget = ref _currentMouseActivation[(int)button];

            if (currentTarget != null)
            {
                FireEventForElement(currentTarget, UIEventType.MouseDeactivate, new UIMouseEvent { Position = mousePosition, Button = button });
                currentTarget.Invoke_OnMouseRelease(button);
            }

            if (newTarget != null)
            {
                FireEventForElement(newTarget, UIEventType.MouseActivate, new UIMouseEvent { Position = mousePosition, Button = button });
                newTarget.Invoke_OnMousePress(button);
            }

            currentTarget = newTarget;
        }

        private UIElement? FindTopNodeAtPosition(Vector2 position)
        {
            return SearchRecursive(position, _dockHost.ActiveWindow!.RootElement);

            static UIElement? SearchRecursive(Vector2 position, UIElement element)
            {
                for (int i = element.Children.Count - 1; i >= 0; --i)
                {
                    UIElement child = element.Children[i];
                    if (child.IsActive && child.ElementTreeBounds.IsWithin(position))
                    {
                        UIElement? ret = SearchRecursive(position, child);
                        if (ret != null)
                            return ret;
                    }
                }

                return element.PixelCoordinates.IsWithin(position) ? element : null;
            }
        }

        public event Action<UIElement, Ref<UIEvent>>? EventFired;
    }
}
