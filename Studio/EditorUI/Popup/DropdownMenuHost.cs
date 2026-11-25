using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Text;
using EditorUI.Assets;
using EditorUI.Common;
using EditorUI.Input;
using EditorUI.Popup.Components;
using EditorUI.Styling;
using EditorUI.Text;
using EditorUI.Visual;
using Primary.Collections.ReadOnly;
using Primary.Common;
using Primary.Mathematics;
using Primary.Windowing;

namespace EditorUI.Popup
{
    public sealed class DropdownMenuHost : PopupHost, IInteractionShape
    {
        private readonly int _index;
        private readonly ImmutableArray<DropdownItem> _options;

        private Int2 _screenPosition;
        private Int2 _screenSize;

        [StyleSetup(StateFlags.SelfInvalidLayout)] private IAssetProvider<FontFamily>? _fontFamily;
        [StyleSetup(StateFlags.SelfInvalidLayout)] private FontStyle _fontStyle;
        [StyleSetup(StateFlags.SelfInvalidLayout)] private FontWeight _fontWeight;

        [StyleSetup(StateFlags.SelfInvalidLayout)] private float _fontSize;

        [StyleInclude] private UIColor _backgroundColor;

        [StyleInclude] private UIColor _strokeColor;
        [StyleInclude] private StrokePosition _strokePosition;
        [StyleInclude] private ushort _strokeWidth;

        internal DropdownMenuHost(Int2 screenPosition, int menuWidth, ROList<string> options, int index)
        {
            _index = index;
            _options = [.. options.Select((option, index) => new DropdownItem(this, option, index))];

            _screenPosition = screenPosition;
            _screenSize = new Int2(menuWidth, 0);

            _fontFamily = null;
            _fontStyle = FontStyle.Normal;
            _fontWeight = FontWeight.Normal;

            _fontSize = 24.0f;

            _backgroundColor = Color.White;
        }

        protected internal override void StartHostingSelf(Window window)
        {
            int menuWidth = Math.Clamp(_screenSize.X, 100, window.ClientSize.X);
            int menuHeight = Math.Min(_options.Length, 10) * ItemHeight + 2;

            Display display = window.Parent?.Display ?? window.Display;

            _screenPosition = Rect.Contain(new Rect(display.UsableBoundaries.Position, display.UsableBoundaries.Size - new Int2(menuWidth, menuHeight)), _screenPosition);
            _screenSize = new Int2(menuWidth, menuHeight);

            if (Math.Max(Math.Abs(window.ClientSize.X - menuWidth), Math.Abs(window.ClientSize.Y - menuHeight)) > 600 || true)
                window.ClientSize = _screenSize;
            else
                window.ClientSize = Int2.Max(window.ClientSize, new Int2(menuWidth, menuHeight));

            window.Position = _screenPosition;

            _screenPosition = Int2.One;
            _screenSize -= new Int2(2);
        }

        protected internal override void CleanupSelf()
        {
            foreach (DropdownItem dropdownItem in _options)
            {
                dropdownItem.DestroySelf();
            }

            base.CleanupSelf();
        }

        protected internal override void UpdateSelf(Window window)
        {
        }

        protected internal override void PaintSelf(ref readonly PainterContext painter, Window window)
        {
            Int2 screenMax = _screenPosition + _screenSize;

            if (_backgroundColor.IsVisible || (_strokeWidth > 0 && _strokeColor.IsVisible))
                painter.AddRectangle(new Boundaries(_screenPosition.AsVector2(), screenMax.AsVector2()), new Paint(_backgroundColor, _strokeColor, _strokeWidth, _strokePosition));

            for (int i = 0; i < _options.Length; ++i)
            {
                DropdownItem item = _options[i];

                Vector2 minimum = new Vector2(_screenPosition.X, _screenPosition.Y + i * ItemHeight);

                item.PaintSelf(in painter, new Boundaries(minimum, minimum + new Vector2(_screenSize.X, ItemHeight)));
            }
        }

        public override bool HandleEventSelf(ref readonly UIInputEvent inputEvent)
        {
            return true;
        }

        internal void SelectNewIndex(int newIndex)
        {
            if (_index != newIndex)
            {
                OnIndexSelected?.Invoke(newIndex);
            }

            UIManager.Instance.PopupManager.ClosePopup(this);
        }

        [StyleUpdateCallback(nameof(FontFamily), nameof(FontStyle), nameof(FontWeight), nameof(FontSize))]
        private void TextStylingDataCallback()
        {
            foreach (DropdownItem dropdownItem in _options)
            {
                dropdownItem.CleanupSelf();
            }
        }

        public override IInteractable GetInteractable(Vector2 point)
        {
            int rowIndex = (int)((point.Y - _screenPosition.Y) / ItemHeight);
            if (rowIndex >= 0 && rowIndex < _options.Length)
                return _options[rowIndex];
            return this;
        }

        public bool Intersects(Vector2 point)
        {
            return point.X >= _screenPosition.X && point.Y >= _screenPosition.Y && point.X <= (_screenPosition.X + _screenSize.X) && point.Y <= (_screenPosition.Y + _screenSize.Y);
        }

        protected internal override void GetUnstyledObjects(ref StyleQueueContext context)
        {
            foreach (DropdownItem item in _options)
            {
                context.TryEnqueue(item);
            }
        }

        public int Index => _index;

        public override IInteractionShape? Shape => this;

        #region Serializable
        public IAssetProvider<FontFamily>? FontFamily { get => _fontFamily; set => SetStyledField(value); }
        public FontStyle FontStyle { get => _fontStyle; set => SetStyledField(value); }
        public FontWeight FontWeight { get => _fontWeight; set => SetStyledField(value); }

        public float FontSize { get => _fontSize; set => SetStyledField(value); }

        public UIColor BackgroundColor { get => _backgroundColor; set => SetStyledField(value); }

        public UIColor StrokeColor { get => _strokeColor; set => SetStyledField(value); }
        public StrokePosition StrokePosition { get => _strokePosition; set => SetStyledField(value); }
        public ushort StrokeWidth { get => _strokeWidth; set => SetStyledField(value); }
        #endregion

        public event Action<int>? OnIndexSelected;

        internal const int ItemHeight = 24;
    }
}
