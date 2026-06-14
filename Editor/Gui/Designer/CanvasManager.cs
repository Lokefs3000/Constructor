using Editor.UI;
using Editor.UI.Assets;
using Editor.UI.Datatypes;
using Editor.UI.Designer;
using Editor.UI.Elements;
using Editor.UI.Elements.Tree;
using Editor.UI.Interaction;
using Editor.UI.Reflection;
using Primary.Assets;
using Primary.Common;
using Primary.Input.Devices;
using Primary.Mathematics;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Editor.Gui.Designer
{
    internal sealed class CanvasManager
    {
        private readonly UIDesigner _designer;

        private readonly UICanvas _canvas;
        private readonly UIElement _interaction;

        private CanvasWindow _hostedWindow;
        private UIFrame _rootElement;

        private UIFrame _outlineElement;
        private UIImage _gridElement;

        private Type? _activeElementType;
        private UIElement? _activeElement;

        private float _snappingScale;

        internal CanvasManager(UIDesigner designer)
        {
            _designer = designer;

            _canvas = designer.FindElementWithId<UICanvas>("MainView") ?? throw new Exception("Failed to find main canvas");
            _interaction = _canvas.FindElementWithId<UIElement>("Interaction") ?? throw new Exception();

            _hostedWindow = UIManager.Instance.OpenWindow<CanvasWindow>(null, null);
            _rootElement = new UIFrame(_hostedWindow.RootElement)
            {
                IsActive = false,

                Anchor = Vector2.Zero,
                Position = UIValue2.Zero,
                Size = UIValue2.Max,

                BackgroundColor = Color.FromHex("242424"),

                StrokeWeight = 0.0f
            };

            _outlineElement = new UIFrame(_canvas)
            {
                IsActive = false,

                Anchor = Vector2.Zero,
                Position = UIValue2.Zero,
                Size = new UIValue2(200, 200),

                BackgroundColor = Color.TransparentBlack,

                StrokeColor = new Color(0.3f, 0.25f, 0.2f),
                StrokeWeight = 2.0f,
            };
            _gridElement = _canvas.FindElementWithId<UIImage>("ViewGrid") ?? throw new NullReferenceException();

            _activeElementType = null;
            _activeElement = null;

            _snappingScale = 10.0f;

            _canvas.HostedWindow = _hostedWindow;

            //callbacks
            _designer.LayoutRecalculated += LayoutRecalculated;

            _canvas.OnMouseMove += CanvasMouseMotion;
            _canvas.OnDragStart += CanvasDragCallback;

            _interaction.OnMouseMove += CanvasMouseMotion;
            _interaction.OnMouseActivate += InteractionMouseRelease;
        }

        internal void ClearView()
        {
            _canvas.ClientSize = new Int2(200);
            _hostedWindow.ClientSize = new Int2(200);

            _canvas.ClientOffset = new Vector2(100.0f);
            _outlineElement.Position = new UIValue2(100, 100);

            _interaction.Position = _outlineElement.Position;
            _interaction.Size = _outlineElement.Size;

            _gridElement.Position = _outlineElement.Position;
            _gridElement.Size = _outlineElement.Size;
            SetGridUVs();

            _rootElement.ClearChildren();
        }

        private void LayoutRecalculated()
        {
            
        }

        private void CanvasDragCallback(OnDragStartContext context)
        {
            Vector2 clientOffset = _canvas.ClientOffset;

            context.SetDragCallback((args) =>
            {
                _canvas.ClientOffset = clientOffset + args.Delta;

                _outlineElement.Position = new UIValue2((int)_canvas.ClientOffset.X, (int)_canvas.ClientOffset.Y);
                _gridElement.Position = _outlineElement.Position;
                _interaction.Position = _outlineElement.Position;
            });
        }

        private void CanvasMouseMotion(Vector2 position)
        {
            if (_activeElementType != null)
            {
                if (_activeElement == null)
                {
                    CachedElementData elementData = UIManager.Instance.ReflectionManager.ElementCache.GetElementData(_activeElementType);

                    _activeElement = (UIElement)elementData.Constructor!.Invoke(null);
                    _activeElement.IsActive = false;
                    _activeElement.Size = new UIValue2(100, 100);

                    _interaction.AddChild(_activeElement);
                }
                else if (_activeElement.GetType() != _activeElementType)
                {
                    _activeElement.Destroy();
                    _activeElement = null;

                    CachedElementData elementData = UIManager.Instance.ReflectionManager.ElementCache.GetElementData(_activeElementType);
                    
                    _activeElement = (UIElement)elementData.Constructor!.Invoke(null);
                    _activeElement.IsActive = false;
                    _activeElement.Size = new UIValue2(100, 100);

                    _interaction.AddChild(_activeElement);
                }
            }

            if (_activeElement != null)
            {
                position -= _canvas.PixelCoordinates.Minimum + _canvas.ClientOffset;
                position = Vector2.Round(position / _snappingScale) * _snappingScale;

                _activeElement.Position = new UIValue2((int)position.X, (int)position.Y);
            }
        }

        private void InteractionMouseRelease(MouseButton button)
        {
            if (button == MouseButton.Left && _activeElement != null && _activeElementType != null)
            {
                CachedElementData elementData = UIManager.Instance.ReflectionManager.ElementCache.GetElementData(_activeElementType);

                Vector2 position = new Vector2(_activeElement.Position.X.Absolute, _activeElement.Position.Y.Absolute);
                UIElement element = _hostedWindow.Raycast<UIElement>(position)!;

                UIElement newElement = (UIElement)elementData.Constructor!.Invoke(null);

                newElement.Position = new UIValue2((int)position.X, (int)position.Y);
                newElement.Size = new UIValue2(100, 100);

                element.AddChild(newElement);
                _designer.HierchyManager.AddHierchyElement(newElement);
            }
        }

        private void SetGridUVs()
        {
            float scale = 1.0f / 64.0f / (_snappingScale / 64.0f);
            Vector2 local = new Vector2(_outlineElement.Size.X.Absolute, _outlineElement.Size.Y.Absolute);

            _gridElement.UVMax = local * scale;
        }

        internal void SetActiveElementType(Type type)
        {
            if (type.IsAssignableTo(typeof(UIElement)))
                _activeElementType = type;
        }

        internal UIFrame RootElement => _rootElement;
    }
}
