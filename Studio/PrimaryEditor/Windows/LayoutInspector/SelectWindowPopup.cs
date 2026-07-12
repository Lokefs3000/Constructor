using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using EditorUI.Dock;
using EditorUI.Input;
using EditorUI.Mathematics;
using EditorUI.Text;
using EditorUI.Widgets;
using EditorUI.Windowing;
using Primary.Input.Devices;
using Primary.Mathematics;
using PrimaryEditor.Core;

namespace PrimaryEditor.Windows.LayoutInspector
{
    internal sealed class SelectWindowPopup : IDisposable
    {
        private readonly LayoutInspectorWindow _window;

        private Widget? _rootWidget;
        private LayoutFrame? _listWidget;
        private Widget? _previewBackground;
        private Widget? _previewRect;

        private readonly Dictionary<WidgetWindow, Button> _windowButtons;

        private bool _disposedValue;

        internal SelectWindowPopup(LayoutInspectorWindow window)
        {
            _window = window;

            _windowButtons = new Dictionary<WidgetWindow, Button>();
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {

                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        internal void Initialize()
        {
            _rootWidget = _window.RootWidget.FindWidgetWithId<Widget>("sel-window-popup", true)!;
            _listWidget = _rootWidget.FindWidgetWithId<LayoutFrame>("list", true)!;

            Widget previewContainer = _rootWidget.FindWidgetWithId<Widget>("preview", true)!;
            _previewBackground = previewContainer.FindWidgetWithId<Widget>("background", true)!;
            _previewRect = _previewBackground.FindWidgetWithId<Widget>("rect", true);

            _rootWidget.OnMousePress += (_, inputEvent) =>
            {
                if (inputEvent.Button == MouseButton.Left)
                {
                    _rootWidget.IsEnabled = false;
                }
            };

            _rootWidget.IsEnabled = false;
        }

        internal void Cleanup()
        {
            _windowButtons.Clear();

            _rootWidget = null;
            _listWidget = null;

            _previewBackground = null;
            _previewRect = null;
        }

        internal void Show()
        {
            _rootWidget!.IsEnabled = true;
            _previewBackground!.IsEnabled = false;

            VerifyOpenWindow();
        }

        private void VerifyOpenWindow()
        {
            foreach (WindowBase window in EditorRuntime.Instance.UIManager.WindowManager.Windows)
            {
                if (window is WidgetWindow widgetWindow)
                {
                    if (!_windowButtons.ContainsKey(widgetWindow))
                    {
                        Button button = new Button()
                        {
                            Parent = _listWidget,
                            Size = new UIValue2(1.0f, 0, 0.0f, 32),
                        };

                        Label label = new Label()
                        {
                            Parent = button,
                            Size = UIValue2.Max,
                            Alignment = TextAlignment.CenterLeft,
                            AllowRichText = false,
                            InputState = WidgetInputState.Never,
                            WrapMode = TextWrapMode.Ellipsis,

                            Text = widgetWindow.GetType().Name
                        };

                        button.OnMouseHover += (_, inputEvent, hasEntered) => OnShowWindowPreview(widgetWindow, inputEvent, hasEntered);
                        button.OnMousePress += (_, inputEvent) =>
                        {
                            if (inputEvent.Button == MouseButton.Left)
                            {
                                _rootWidget!.IsEnabled = false;
                                _window.SetTargetWindow(widgetWindow);
                            }
                        };
                    }
                }
            }
        }

        private void OnShowWindowPreview(WidgetWindow window, UIMouseInputEvent inputEvent, bool hasEntered)
        {
            WindowDock? dock = window.Parent as WindowDock;
            DockHost? host = dock?.Host;

            if (host != null && dock != null)
            {
                Vector2 idealSize = _previewBackground!.Parent!.IdealSize;
                if (host.OwnedWindow.ClientSize.X > host.OwnedWindow.ClientSize.Y)
                {
                    if (idealSize.X < idealSize.Y)
                        _previewBackground.Size = new UIValue2(1.0f, (idealSize.Y / idealSize.X) * (host.OwnedWindow.ClientSize.Y / (float)host.OwnedWindow.ClientSize.X));
                    else
                        _previewBackground.Size = new UIValue2((idealSize.X / idealSize.Y) * (host.OwnedWindow.ClientSize.Y / (float)host.OwnedWindow.ClientSize.X), 1.0f);
                }
                else
                {
                    if (idealSize.X < idealSize.Y)
                        _previewBackground.Size = new UIValue2(1.0f, (idealSize.Y / idealSize.X) * (host.OwnedWindow.ClientSize.X / (float)host.OwnedWindow.ClientSize.Y));
                    else
                        _previewBackground.Size = new UIValue2((idealSize.X / idealSize.Y) * (host.OwnedWindow.ClientSize.X / (float)host.OwnedWindow.ClientSize.Y), 1.0f);
                }

                Vector2 relativeOffset = window.WindowRect.Position.AsVector2() / host.HostRect.Size.AsVector2();
                Vector2 relativeSize = window.WindowRect.Size.AsVector2() / host.HostRect.Size.AsVector2();

                _previewRect!.Position = new UIValue2(relativeOffset.X, relativeOffset.Y);
                _previewRect!.Size = new UIValue2(relativeSize.X, relativeSize.Y);

                _previewBackground!.IsEnabled = true;
                return;
            }

            _previewBackground!.IsEnabled = false;
        }
    }
}
